// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets a system user's permission level by assigning one of the role levels
/// (Admin, Authorised, User). The role is applied exclusively: granting Admin removes
/// Authorised and vice versa; assigning User removes both elevated roles. Permission sets
/// are derived from the role via <see cref="Klacks.Api.Domain.Constants.Permissions"/>.
/// The actual role mutation runs through <see cref="Klacks.Api.Application.Commands.Accounts.ChangeRoleCommand"/>,
/// once per grant/revoke. After the grant/revoke calls complete, the resulting role set is re-read
/// fresh via <see cref="Klacks.Api.Domain.Interfaces.Authentification.IUserManagementService.GetUserRolesAsync"/>
/// (a string-projection query, unaffected by EF Core entity tracking) and compared against what was
/// requested; a mismatch is reported as an error. This is NOT wrapped in a single
/// <c>IUnitOfWork.ExecuteInTransactionAsync</c> call: <c>ChangeRoleCommandHandler</c> already owns its
/// own transaction per call, so nesting another transaction around it would attempt to begin a second
/// transaction on the same DbContext and fail. Consequently a verification mismatch here cannot roll
/// back already-committed grants/revokes automatically — each individual role change is durable on its
/// own, only the combined end state is checked afterwards. Before anything is written, a demotion of the last active
/// administrator is refused with <see cref="Klacks.Api.Application.Services.Authentication.LastAdministratorRule"/>, so no
/// half-applied grant is left behind; the user management service enforces the same rule on every role removal.
/// </summary>
/// <param name="userId">Optional. The target user's id. Preferred over userName when known.</param>
/// <param name="userName">Optional. Name, e-mail or login used to resolve the target user when userId is omitted.</param>
/// <param name="role">Required. The role level to assign: Admin, Authorised or User.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Services.Authentication;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("assign_user_permissions")]
public class AssignUserPermissionsSkill : BaseSkillImplementation
{
    private static readonly IReadOnlyList<string> ElevatedRoles = new[] { Roles.Admin, Roles.Authorised };

    private readonly IMediator _mediator;
    private readonly IUserManagementService _userManagementService;

    public AssignUserPermissionsSkill(
        IMediator mediator,
        IUserManagementService userManagementService)
    {
        _mediator = mediator;
        _userManagementService = userManagementService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var roleInput = GetParameter<string>(parameters, "role");
        var role = AssignableRoleParser.Normalize(roleInput);
        if (role == null)
        {
            return SkillResult.Error(AssignableRoleParser.InvalidRoleMessage(roleInput));
        }

        var resolution = await SystemUserResolver.ResolveAsync(
            _userManagementService, GetParameter<string>(parameters, "userId"), GetParameter<string>(parameters, "userName"));
        if (resolution.User == null)
        {
            return SkillResult.Error(resolution.ErrorMessage(listCandidates: true));
        }

        var user = resolution.User;

        if (role != Roles.Admin && await WouldRemoveLastActiveAdministratorAsync(user))
        {
            return SkillResult.Error(LastAdministratorRule.RefusalMessage);
        }

        var rolesToGrant = role == Roles.User
            ? Array.Empty<string>()
            : new[] { role };
        var rolesToRevoke = ElevatedRoles.Where(r => !rolesToGrant.Contains(r)).ToArray();

        try
        {
            foreach (var roleToGrant in rolesToGrant)
            {
                await ApplyRoleAsync(user.Id, roleToGrant, true, cancellationToken);
            }

            foreach (var roleToRevoke in rolesToRevoke)
            {
                await ApplyRoleAsync(user.Id, roleToRevoke, false, cancellationToken);
            }
        }
        catch (ConflictException ex)
        {
            return SkillResult.Error($"Failed to assign role '{role}': {ex.Message}");
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var actualRoles = await _userManagementService.GetUserRolesAsync(user);
        var otherElevatedRole = role == Roles.Admin ? Roles.Authorised : Roles.Admin;
        var verified = role == Roles.User
            ? !actualRoles.Contains(Roles.Admin) && !actualRoles.Contains(Roles.Authorised)
            : actualRoles.Contains(role) && !actualRoles.Contains(otherElevatedRole);

        if (!verified)
        {
            return SkillResult.Error(
                $"Database verification failed: user '{fullName}' does not have role '{role}' after the " +
                "write — the role assignment could not be confirmed in the database. Each grant/revoke " +
                "was already committed individually, so this may need manual correction.");
        }

        var permissions = Permissions.GetPermissionsForRole(role).ToList();

        var resultData = new
        {
            UserId = user.Id,
            UserName = fullName,
            Role = role,
            Permissions = permissions
        };

        return SkillResult.SuccessResult(
            resultData,
            $"User '{fullName}' now has role '{role}' with {permissions.Count} permission(s), confirmed in the database (verified).");
    }

    private async Task<bool> WouldRemoveLastActiveAdministratorAsync(AppUser user)
    {
        var holdsAdminRole = (await _userManagementService.GetUserRolesAsync(user)).Contains(Roles.Admin);

        return holdsAdminRole && LastAdministratorRule.WouldRemoveLastActiveAdministrator(
            true, user.DeactivatedAt is null, await _userManagementService.CountActiveAdministratorsAsync());
    }

    private async Task ApplyRoleAsync(string userId, string roleName, bool isSelected, CancellationToken cancellationToken)
    {
        var changeRole = new ChangeRole
        {
            UserId = userId,
            RoleName = roleName,
            IsSelected = isSelected
        };

        await _mediator.Send(new ChangeRoleCommand(changeRole), cancellationToken);
    }
}
