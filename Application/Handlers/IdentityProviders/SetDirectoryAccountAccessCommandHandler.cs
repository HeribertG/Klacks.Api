// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Grants or withdraws directory sign-in for one account of an LDAP/AD provider. There is no linking by e-mail:
/// granting re-reads the entry from the directory and then either keeps the account linked via (provider, external id)
/// - reactivating it only when it holds no role - or moves a role-less link left on a soft-deleted provider (GUID ids
/// only; an account with a role is refused, so no role is handed over), or
/// creates a new account without role and password; an e-mail already used by any account refuses the grant.
/// Withdrawing deactivates the linked account; nothing linked is refused. Deactivation and reactivation go through
/// DeactivateAccountCommand and ReactivateAccountCommand, so the self-deactivation check, the last-administrator rule
/// and DeactivatedBy apply. Every change writes one structured audit log line (actor, target, provider, action).
/// </summary>
/// <param name="providerRepository">Loads the provider</param>
/// <param name="ldapService">Re-reads the directory entry</param>
/// <param name="userReader">Finds linked accounts and re-reads the account after the change</param>
/// <param name="userManagementService">Checks the e-mail, reads roles, relinks and creates directory accounts</param>
/// <param name="usernameGenerator">Derives the technical user name of a created account</param>
/// <param name="userService">Supplies the acting administrator's id for the audit line</param>
/// <param name="mediator">Dispatches the deactivation and reactivation commands</param>
/// <param name="logger">Records the operation and the audit lines</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class SetDirectoryAccountAccessCommandHandler : BaseHandler, IRequestHandler<SetDirectoryAccountAccessCommand, DirectoryAccountResource>
{
    public const string ActionCreated = "created";
    public const string ActionReactivated = "reactivated";
    public const string ActionDeactivated = "deactivated";
    public const string ActionRelinked = "relinked";

    private const string OperationName = "setting directory account access";
    private const string ProviderNotFoundMessage = "Identity provider not found.";
    private const string AuditMessage =
        "Directory access audit: {Action} account {TargetUserId} on provider {ProviderId} by {ActorUserId}";

    private readonly IIdentityProviderRepository _providerRepository;
    private readonly ILdapService _ldapService;
    private readonly IAppUserReader _userReader;
    private readonly IUserManagementService _userManagementService;
    private readonly IUsernameGeneratorService _usernameGenerator;
    private readonly IUserService _userService;
    private readonly IMediator _mediator;

    public SetDirectoryAccountAccessCommandHandler(
        IIdentityProviderRepository providerRepository,
        ILdapService ldapService,
        IAppUserReader userReader,
        IUserManagementService userManagementService,
        IUsernameGeneratorService usernameGenerator,
        IUserService userService,
        IMediator mediator,
        ILogger<SetDirectoryAccountAccessCommandHandler> logger)
        : base(logger)
    {
        _providerRepository = providerRepository;
        _ldapService = ldapService;
        _userReader = userReader;
        _userManagementService = userManagementService;
        _usernameGenerator = usernameGenerator;
        _userService = userService;
        _mediator = mediator;
    }

    public Task<DirectoryAccountResource> Handle(SetDirectoryAccountAccessCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            var provider = await _providerRepository.Get(request.ProviderId)
                ?? throw new KeyNotFoundException(ProviderNotFoundMessage);

            if (!DirectoryLoginProviders.IsUsableForLogin(provider))
            {
                throw DirectoryAccountAccessRejectedException.ProviderNotUsableForLogin();
            }

            var externalId = LdapUserEntry.NormalizeExternalId(request.ExternalId);

            return request.MayLogin
                ? await GrantAsync(provider, externalId, cancellationToken)
                : await RevokeAsync(provider, externalId, cancellationToken);
        },
        OperationName,
        new { request.ProviderId, request.MayLogin });
    }

    private async Task<DirectoryAccountResource> RevokeAsync(IdentityProvider provider, string externalId, CancellationToken cancellationToken)
    {
        var linked = await _userReader.FindByDirectoryLinkAsync(provider.Id, externalId, cancellationToken)
            ?? throw DirectoryAccountAccessRejectedException.AccountNotLinked();

        if (linked.DeactivatedAt == null)
        {
            await _mediator.Send(new DeactivateAccountCommand(Guid.Parse(linked.Id)), cancellationToken);
            Audit(ActionDeactivated, linked.Id, provider.Id);
        }

        var user = await _userReader.FindByIdNoTrackingAsync(linked.Id, cancellationToken);
        var hasRoles = user != null && (await _userManagementService.GetUserRolesAsync(user)).Count > 0;
        var entry = await TryFindEntryAsync(provider, externalId);

        if (entry != null)
        {
            return DirectoryAccountResourceFactory.FromEntry(entry, provider.Type, user, false, hasRoles);
        }

        return DirectoryAccountResourceFactory.FromLinkedUser(user ?? linked, hasRoles);
    }

    private async Task<DirectoryAccountResource> GrantAsync(IdentityProvider provider, string externalId, CancellationToken cancellationToken)
    {
        var entry = await _ldapService.FindByExternalIdAsync(provider, externalId)
            ?? throw DirectoryAccountAccessRejectedException.AccountNotFound();

        var userId = await KeepLinkedAsync(provider, entry, cancellationToken)
            ?? await RelinkFromDeletedProviderAsync(provider, entry, cancellationToken)
            ?? await CreateAsync(provider, entry);

        var user = await _userReader.FindByIdNoTrackingAsync(userId, cancellationToken);
        var hasRoles = user != null && (await _userManagementService.GetUserRolesAsync(user)).Count > 0;
        return DirectoryAccountResourceFactory.FromEntry(entry, provider.Type, user, false, hasRoles);
    }

    private async Task<string?> KeepLinkedAsync(IdentityProvider provider, LdapUserEntry entry, CancellationToken cancellationToken)
    {
        var linked = await _userReader.FindByDirectoryLinkAsync(provider.Id, entry.StableId, cancellationToken);
        if (linked == null)
        {
            return null;
        }

        await EnsureReactivatableAsync(linked);
        await ReactivateIfNeededAsync(linked, provider.Id, cancellationToken);
        return linked.Id;
    }

    private async Task<string?> RelinkFromDeletedProviderAsync(IdentityProvider provider, LdapUserEntry entry, CancellationToken cancellationToken)
    {
        if (!LdapUserEntry.IsGuidId(entry.StableId))
        {
            return null;
        }

        var orphan = await _userReader.FindLinkedToDeletedProviderAsync(entry.StableId, cancellationToken);
        if (orphan == null)
        {
            return null;
        }

        if ((await _userManagementService.GetUserRolesAsync(orphan)).Count > 0)
        {
            throw DirectoryAccountAccessRejectedException.AccountHasRoles();
        }

        orphan.IdentityProviderId = provider.Id;
        var (updated, updateResult) = await _userManagementService.UpdateUserAsync(orphan);
        if (!updated)
        {
            throw AccountRegistrationRejectedException.FromIdentityErrors(updateResult?.Errors);
        }

        Audit(ActionRelinked, orphan.Id, provider.Id);
        await ReactivateIfNeededAsync(orphan, provider.Id, cancellationToken);
        return orphan.Id;
    }

    private async Task<string> CreateAsync(IdentityProvider provider, LdapUserEntry entry)
    {
        var email = DirectoryAccountResourceFactory.Email(entry)
            ?? throw DirectoryAccountAccessRejectedException.AccountWithoutEmail();

        if (await _userManagementService.FindUserByEmailAsync(email) != null)
        {
            throw DirectoryAccountAccessRejectedException.AccountEmailTaken();
        }

        var firstName = DirectoryAccountResourceFactory.FirstName(entry);
        var lastName = DirectoryAccountResourceFactory.LastName(entry, provider.Type);
        var user = new AppUser
        {
            FirstName = firstName,
            LastName = lastName,
            UserName = await _usernameGenerator.GenerateUniqueUsernameAsync(firstName, lastName),
            Email = email,
            EmailConfirmed = true,
            IdentityProviderId = provider.Id,
            DirectoryExternalId = entry.StableId,
        };

        var (created, createResult) = await _userManagementService.RegisterDirectoryUserAsync(user);
        if (!created)
        {
            if (createResult?.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateEmail)) == true)
            {
                throw DirectoryAccountAccessRejectedException.AccountEmailTaken();
            }

            throw AccountRegistrationRejectedException.FromIdentityErrors(createResult?.Errors);
        }

        Audit(ActionCreated, user.Id, provider.Id);
        return user.Id;
    }

    private async Task EnsureReactivatableAsync(AppUser user)
    {
        if (user.DeactivatedAt != null && (await _userManagementService.GetUserRolesAsync(user)).Count > 0)
        {
            throw DirectoryAccountAccessRejectedException.AccountHasRoles();
        }
    }

    private async Task ReactivateIfNeededAsync(AppUser user, Guid providerId, CancellationToken cancellationToken)
    {
        if (user.DeactivatedAt == null)
        {
            return;
        }

        await _mediator.Send(new ReactivateAccountCommand(Guid.Parse(user.Id)), cancellationToken);
        Audit(ActionReactivated, user.Id, providerId);
    }

    private void Audit(string action, string targetUserId, Guid providerId)
    {
        _logger.LogInformation(AuditMessage, action, targetUserId, providerId, _userService.GetIdString());
    }

    private async Task<LdapUserEntry?> TryFindEntryAsync(IdentityProvider provider, string externalId)
    {
        try
        {
            return await _ldapService.FindByExternalIdAsync(provider, externalId);
        }
        catch (DirectoryUnavailableException ex)
        {
            _logger.LogWarning(ex, "Directory entry could not be re-read after revoking access via provider {ProviderId}", provider.Id);
            return null;
        }
    }
}
