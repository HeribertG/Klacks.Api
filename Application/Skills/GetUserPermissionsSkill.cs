// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that returns the current user's role and full permission list.
/// Use this before attempting operations that require specific permissions to verify access.
/// For basic user identity (name, ID, tenant) use get_user_context instead.
/// </summary>
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_user_permissions")]
public class GetUserPermissionsSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var permissions = context.UserPermissions.ToList();

        var role = DetermineRole(permissions);

        var canCreate = permissions.Any(p => p.StartsWith("CanCreate"));
        var canEdit = permissions.Any(p => p.StartsWith("CanEdit"));
        var canDelete = permissions.Any(p => p.StartsWith("CanDelete"));
        var canAccessSettings = permissions.Contains(Permissions.CanViewSettings) ||
                               permissions.Contains(Permissions.CanEditSettings);

        var resultData = new
        {
            UserId = context.UserId,
            UserName = context.UserName,
            Role = role,
            Permissions = permissions,
            Summary = new
            {
                CanCreateEntities = canCreate,
                CanEditEntities = canEdit,
                CanDeleteEntities = canDelete,
                CanAccessSettings = canAccessSettings,
                TotalPermissions = permissions.Count
            },
            AvailableActions = GetAvailableActions(permissions)
        };

        var message = $"User '{context.UserName}' has role '{role}' with {permissions.Count} permission(s).";

        return Task.FromResult(SkillResult.SuccessResult(resultData, message));
    }

    private static string DetermineRole(List<string> permissions)
    {
        if (permissions.Contains(Permissions.CanEditSettings) ||
            permissions.Contains(Permissions.CanViewSettings))
            return Roles.Admin;

        if (permissions.Contains(Permissions.CanCreateClients) ||
            permissions.Contains(Permissions.CanEditClients) ||
            permissions.Contains(Permissions.CanDeleteClients) ||
            permissions.Contains(Permissions.CanCreateGroups) ||
            permissions.Contains(Permissions.CanEditGroups) ||
            permissions.Contains(Permissions.CanDeleteGroups))
            return Roles.Authorised;

        return Roles.User;
    }

    private static List<string> GetAvailableActions(IReadOnlyList<string> permissions)
    {
        var actions = new List<string>();

        if (permissions.Contains(Permissions.CanViewClients))
            actions.Add("View and search employees/clients");

        if (permissions.Contains(Permissions.CanCreateClients))
            actions.Add("Create new employees/clients");

        if (permissions.Contains(Permissions.CanEditClients))
            actions.Add("Edit existing employees/clients");

        if (permissions.Contains(Permissions.CanViewGroups))
            actions.Add("View groups and organizational units");

        if (permissions.Contains(Permissions.CanEditGroups) || permissions.Contains(Permissions.CanCreateGroups))
            actions.Add("Manage group memberships");

        if (permissions.Contains(Permissions.CanViewContracts))
            actions.Add("View contracts");

        if (permissions.Contains(Permissions.CanCreateContracts) || permissions.Contains(Permissions.CanEditContracts))
            actions.Add("Assign and manage contracts");

        if (permissions.Contains(Permissions.CanViewShifts))
            actions.Add("View shifts");

        if (permissions.Contains(Permissions.CanViewSchedule))
            actions.Add("View schedule");

        if (permissions.Contains(Permissions.CanPlan))
            actions.Add("Plan and organize schedules");

        if (permissions.Contains(Permissions.CanEditSchedule))
            actions.Add("Edit schedule entries");

        if (permissions.Contains(Permissions.CanEditSettings))
            actions.Add("Access and modify system settings");

        return actions;
    }
}
