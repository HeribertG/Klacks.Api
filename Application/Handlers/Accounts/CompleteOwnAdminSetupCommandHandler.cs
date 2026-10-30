// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Replaces the seeded admin of a fresh installation with the operator's own admin account, in one transaction:
/// register the new account, grant it the Admin role, then deactivate the seeded admin. The user name is always
/// generated server-side from first and last name, the same convention the user administration uses; a user
/// name sent by the client is ignored. A rejected registration is reported with Identity's reason codes (400);
/// 409 is reserved for "already completed" and "not required in this environment". The administrator-role lock is taken
/// before the seeded admin is deactivated, so the last-administrator count is read under the same lock every other
/// deactivation uses.
/// </summary>
/// <param name="adminSetupGateService">Tells whether the setup is still required in this installation</param>
/// <param name="userManagementService">Registers the account, grants the role and deactivates the seeded admin</param>
/// <param name="administratorRoleLock">Serializes the deactivation of the seeded admin against other administrator changes</param>
/// <param name="usernameGeneratorService">Derives the unique user name from first and last name</param>
/// <param name="authMapper">Maps the registration form onto the new AppUser</param>
/// <param name="unitOfWork">Wraps the three steps in one transaction, so the seeded admin stays active on any failure</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class CompleteOwnAdminSetupCommandHandler : BaseHandler, IRequestHandler<CompleteOwnAdminSetupCommand, bool>
{
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly IUserManagementService _userManagementService;
    private readonly IAdministratorRoleLock _administratorRoleLock;
    private readonly IUsernameGeneratorService _usernameGeneratorService;
    private readonly AuthMapper _authMapper;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOwnAdminSetupCommandHandler(
        IAdminSetupGateService adminSetupGateService,
        IUserManagementService userManagementService,
        IAdministratorRoleLock administratorRoleLock,
        IUsernameGeneratorService usernameGeneratorService,
        AuthMapper authMapper,
        IUnitOfWork unitOfWork,
        ILogger<CompleteOwnAdminSetupCommandHandler> logger)
        : base(logger)
    {
        _adminSetupGateService = adminSetupGateService;
        _userManagementService = userManagementService;
        _administratorRoleLock = administratorRoleLock;
        _usernameGeneratorService = usernameGeneratorService;
        _authMapper = authMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CompleteOwnAdminSetupCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _administratorRoleLock.AcquireAsync(cancellationToken);

            if (await _adminSetupGateService.IsExemptAsync())
            {
                throw new ConflictException(AdminSetupMessages.NotRequiredInEnvironment);
            }

            if (!await _adminSetupGateService.IsGateActiveAsync())
            {
                throw new ConflictException(AdminSetupMessages.AlreadyCompleted);
            }

            var registration = request.NewAdmin
                ?? throw new InvalidRequestException(AccountRegistrationMessages.RegistrationRequired);
            var firstName = RequireName(registration.FirstName, AccountRegistrationMessages.FirstNameRequired);
            var lastName = RequireName(registration.LastName, AccountRegistrationMessages.LastNameRequired);

            if (!EmailAddressFormat.TryNormalize(registration.Email, out var email))
            {
                throw AccountRegistrationRejectedException.InvalidEmail();
            }

            var newAdmin = _authMapper.ToAppUser(registration);
            newAdmin.FirstName = firstName;
            newAdmin.LastName = lastName;
            newAdmin.Email = email;
            newAdmin.UserName = await _usernameGeneratorService.GenerateUniqueUsernameAsync(firstName, lastName);

            var (registered, identityResult) = await _userManagementService.RegisterUserAsync(
                newAdmin, registration.Password ?? string.Empty);

            if (!registered)
            {
                var rejection = AccountRegistrationRejectedException.FromIdentityErrors(identityResult?.Errors);
                _logger.LogWarning(
                    "Own-admin setup registration rejected for {Email}: {Reasons}",
                    email.MaskEmail(), string.Join(',', rejection.Reasons));
                throw rejection;
            }

            var (roleAssigned, roleMessage) = await _userManagementService.ChangeUserRoleAsync(newAdmin.Id, Roles.Admin, true);
            if (!roleAssigned)
            {
                _logger.LogError("Own-admin setup: failed to grant Admin role to {Email}: {Message}", email.MaskEmail(), roleMessage);
                throw new InvalidOperationException(AdminSetupMessages.AdminRoleNotGranted);
            }

            var (deactivated, deactivationMessage) = await _userManagementService.DeactivateUserAsync(
                Guid.Parse(SystemAccounts.SeedAdminUserId), newAdmin.Id);
            if (!deactivated)
            {
                _logger.LogError("Own-admin setup: failed to deactivate the seed admin account: {Message}", deactivationMessage);
                throw new InvalidOperationException(AdminSetupMessages.SeedAdminNotDeactivated);
            }

            _logger.LogInformation("Own-admin setup completed: {Email} is now admin, seed admin deactivated", email.MaskEmail());
            return true;
        });
    }

    private static string RequireName(string? value, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidRequestException(missingMessage);
        }

        return value.Trim();
    }
}
