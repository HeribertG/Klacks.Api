// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the directory approval list of one LDAP/AD provider: searches the directory (server-side, capped at
/// DirectoryAccountLimits.ListLimit), marks each row with its linked account (and whether it holds roles) or, when
/// unlinked, whether its e-mail is already used by a Klacks account, and - only for an unfiltered, complete result -
/// appends linked active accounts the directory no longer returns. A directory that cannot be read answers Success=false instead of an error.
/// </summary>
/// <param name="providerRepository">Loads the provider</param>
/// <param name="ldapService">Searches the directory</param>
/// <param name="userReader">Reads linked accounts, their role holders and the e-mails already in use</param>
/// <param name="logger">Records the operation</param>

using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class ListDirectoryAccountsQueryHandler : BaseHandler, IRequestHandler<ListDirectoryAccountsQuery, DirectoryAccountListResource>
{
    private const string OperationName = "listing directory accounts";
    private const string ProviderNotFoundMessage = "Identity provider not found.";

    private readonly IIdentityProviderRepository _providerRepository;
    private readonly ILdapService _ldapService;
    private readonly IAppUserReader _userReader;

    public ListDirectoryAccountsQueryHandler(
        IIdentityProviderRepository providerRepository,
        ILdapService ldapService,
        IAppUserReader userReader,
        ILogger<ListDirectoryAccountsQueryHandler> logger)
        : base(logger)
    {
        _providerRepository = providerRepository;
        _ldapService = ldapService;
        _userReader = userReader;
    }

    public Task<DirectoryAccountListResource> Handle(ListDirectoryAccountsQuery request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            var provider = await _providerRepository.Get(request.ProviderId)
                ?? throw new KeyNotFoundException(ProviderNotFoundMessage);

            if (!DirectoryLoginProviders.IsUsableForLogin(provider))
            {
                throw DirectoryAccountAccessRejectedException.ProviderNotUsableForLogin();
            }

            LdapUserSearchResult searchResult;
            try
            {
                searchResult = await _ldapService.SearchAccountsAsync(provider, request.Search, DirectoryAccountLimits.ListLimit);
            }
            catch (DirectoryUnavailableException ex)
            {
                _logger.LogWarning(ex, "Directory approval list could not read provider {ProviderId}", provider.Id);
                return new DirectoryAccountListResource { Success = false };
            }

            var accounts = await BuildRowsAsync(provider, searchResult, string.IsNullOrWhiteSpace(request.Search), cancellationToken);

            return new DirectoryAccountListResource
            {
                Success = true,
                IsTruncated = !searchResult.IsComplete,
                Accounts = accounts,
            };
        },
        OperationName,
        new { request.ProviderId });
    }

    private async Task<List<DirectoryAccountResource>> BuildRowsAsync(
        IdentityProvider provider, LdapUserSearchResult searchResult, bool isUnfiltered, CancellationToken cancellationToken)
    {
        var linkedUsers = await _userReader.GetByIdentityProviderAsync(provider.Id, cancellationToken);
        var linkedByExternalId = linkedUsers
            .Where(user => user.DirectoryExternalId != null)
            .GroupBy(user => user.DirectoryExternalId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var userIdsWithRoles = await _userReader.GetUserIdsWithRolesAsync(
            linkedUsers.Select(user => user.Id).ToList(), cancellationToken);

        var unlinkedEmails = searchResult.Entries
            .Where(entry => !linkedByExternalId.ContainsKey(entry.StableId))
            .Select(DirectoryAccountResourceFactory.Email)
            .OfType<string>()
            .ToList();

        var emailsInUse = await _userReader.GetEmailsInUseAsync(unlinkedEmails, cancellationToken);

        var rows = new List<DirectoryAccountResource>(searchResult.Entries.Count);
        var listedExternalIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in searchResult.Entries)
        {
            listedExternalIds.Add(entry.StableId);
            linkedByExternalId.TryGetValue(entry.StableId, out var linkedUser);

            var email = DirectoryAccountResourceFactory.Email(entry);
            var emailInUse = email != null && emailsInUse.Contains(email);
            var hasRoles = linkedUser != null && userIdsWithRoles.Contains(linkedUser.Id);

            rows.Add(DirectoryAccountResourceFactory.FromEntry(entry, provider.Type, linkedUser, emailInUse, hasRoles));
        }

        if (isUnfiltered && searchResult.IsComplete)
        {
            rows.AddRange(linkedUsers
                .Where(user => user.DeactivatedAt == null
                    && user.DirectoryExternalId != null
                    && !listedExternalIds.Contains(user.DirectoryExternalId))
                .Select(user => DirectoryAccountResourceFactory.FromLinkedUser(user, userIdsWithRoles.Contains(user.Id))));
        }

        return rows
            .OrderBy(row => row.DisplayName, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(row => row.ExternalId, StringComparer.Ordinal)
            .ToList();
    }
}
