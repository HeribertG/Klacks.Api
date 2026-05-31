// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets a system user's permission level by assigning one of the role levels
/// (Admin, Authorised, User). The role is applied exclusively: granting Admin removes
/// Authorised and vice versa; assigning User removes both elevated roles. Permission sets
/// are derived from the role via <see cref="Klacks.Api.Domain.Constants.Permissions"/>.
/// The actual role mutation runs through <see cref="Klacks.Api.Application.Commands.Accounts.ChangeRoleCommand"/>.
/// </summary>
/// <param name="userId">Optional. The target user's id. Preferred over userName when known.</param>
/// <param name="userName">Optional. Name, e-mail or login used to resolve the target user when userId is omitted.</param>
/// <param name="role">Required. The role level to assign: Admin, Authorised or User.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
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
        var role = NormalizeRole(roleInput);
        if (role == null)
        {
            return SkillResult.Error(
                $"Invalid role '{roleInput}'. Allowed values are: {Roles.Admin}, {Roles.Authorised}, {Roles.User}.");
        }

        var (user, resolveError) = await ResolveUserAsync(parameters);
        if (user == null)
        {
            return SkillResult.Error(resolveError!);
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

        var permissions = Permissions.GetPermissionsForRole(role).ToList();
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        var resultData = new
        {
            UserId = user.Id,
            UserName = fullName,
            Role = role,
            Permissions = permissions
        };

        return SkillResult.SuccessResult(
            resultData,
            $"User '{fullName}' now has role '{role}' with {permissions.Count} permission(s).");
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

    private async Task<(AppUser? User, string? Error)> ResolveUserAsync(Dictionary<string, object> parameters)
    {
        var userId = GetParameter<string>(parameters, "userId");
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var byId = await _userManagementService.FindUserByIdAsync(userId);
            return byId == null
                ? (null, $"User with id '{userId}' not found.")
                : (byId, null);
        }

        var userName = GetParameter<string>(parameters, "userName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return (null, "Either userId or userName must be provided.");
        }

        var matches = (await _userManagementService.GetUserListAsync())
            .Where(u =>
                u.FirstName.Contains(userName, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(userName, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(userName, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase) ||
                $"{u.FirstName} {u.LastName}".Contains(userName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return (null, $"No user found matching '{userName}'.");
        }

        if (matches.Count > 1)
        {
            var candidates = string.Join(", ", matches.Select(u => $"{u.FirstName} {u.LastName} ({u.Email})"));
            return (null, $"'{userName}' is ambiguous. Matching users: {candidates}. Provide userId instead.");
        }

        var resolved = await _userManagementService.FindUserByIdAsync(matches[0].Id);
        return resolved == null
            ? (null, $"User '{userName}' could not be loaded.")
            : (resolved, null);
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        if (role.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Roles.Admin;
        }

        if (role.Equals(Roles.Authorised, StringComparison.OrdinalIgnoreCase))
        {
            return Roles.Authorised;
        }

        if (role.Equals(Roles.User, StringComparison.OrdinalIgnoreCase))
        {
            return Roles.User;
        }

        return null;
    }
}
