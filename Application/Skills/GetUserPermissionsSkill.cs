using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetUserPermissionsSkill : BaseSkill
{
    public override string Name => "get_user_permissions";

    public override string Description =>
        "Returns the current user's permissions and role. Use this to understand what actions the user is allowed to perform " +
        "before attempting operations that require specific permissions.";

    public override SkillCategory Category => SkillCategory.System;

    public override IReadOnlyList<string> RequiredPermissions => Array.Empty<string>();

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

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
        if (permissions.Contains(Permissions.CanEditSettings))
            return Roles.Admin;

        if (permissions.Contains(Permissions.CanDeleteClients) ||
            permissions.Contains(Permissions.CanDeleteGroups))
            return Roles.Admin;

        if (permissions.Contains(Permissions.CanEditClients) ||
            permissions.Contains(Permissions.CanCreateClients))
            return Roles.Authorised;

        return Roles.Authorised;
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
