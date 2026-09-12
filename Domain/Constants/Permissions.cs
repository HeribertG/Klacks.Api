// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class Permissions
{
    public const string CanViewClients = "CanViewClients";
    public const string CanCreateClients = "CanCreateClients";
    public const string CanEditClients = "CanEditClients";
    public const string CanDeleteClients = "CanDeleteClients";

    public const string CanViewGroups = "CanViewGroups";
    public const string CanCreateGroups = "CanCreateGroups";
    public const string CanEditGroups = "CanEditGroups";
    public const string CanDeleteGroups = "CanDeleteGroups";

    public const string CanViewContracts = "CanViewContracts";
    public const string CanCreateContracts = "CanCreateContracts";
    public const string CanEditContracts = "CanEditContracts";
    public const string CanDeleteContracts = "CanDeleteContracts";

    public const string CanViewShifts = "CanViewShifts";
    public const string CanCreateShifts = "CanCreateShifts";
    public const string CanEditShifts = "CanEditShifts";
    public const string CanDeleteShifts = "CanDeleteShifts";

    public const string CanViewSettings = "CanViewSettings";
    public const string CanEditSettings = "CanEditSettings";

    public const string CanPlan = "CanPlan";
    public const string CanViewSchedule = "CanViewSchedule";
    public const string CanEditSchedule = "CanEditSchedule";

    public const string CanUseAssistant = "CanUseAssistant";

    public const string CanEditClientNotes = "CanEditClientNotes";

    /// <summary>
    /// Governs the unattended behaviour of the assistant: autonomy level, proactive governance,
    /// recurring (cron) tasks and multi-step plans. Deliberately outside the Planer floor — CanPlan
    /// means "may plan shifts", not "may decide what the agent is allowed to do without being asked".
    /// </summary>
    public const string CanManageAutomation = "CanManageAutomation";

    /// <summary>
    /// Mints and revokes personal access tokens, the credential external MCP clients authenticate
    /// with. Deliberately outside the Planer floor: a long-lived credential is a security boundary of
    /// its own, independent of scheduling rights.
    /// </summary>
    public const string CanManageAccessTokens = "CanManageAccessTokens";

    private static readonly string[] PlannerFloorPermissions =
    [
        CanViewClients, CanViewGroups, CanViewContracts, CanViewShifts,
        CanViewSchedule, CanEditSchedule, CanPlan,
        CanEditClientNotes, CanUseAssistant
    ];

    /// <summary>
    /// The rights every authenticated caller holds even without a role (display name "Planer"): read
    /// access to the core lists plus schedule/work/absence/note editing. Single source for the two
    /// paths that reach it — an unrecognised role string and an empty role list — so the floor can
    /// never drift apart between them.
    /// </summary>
    public static IReadOnlyList<string> PlannerFloor => PlannerFloorPermissions;

    public static IReadOnlyList<string> GetPermissionsForRole(string role)
    {
        return role switch
        {
            Roles.Admin => new[]
            {
                CanViewClients, CanCreateClients, CanEditClients, CanDeleteClients,
                CanViewGroups, CanCreateGroups, CanEditGroups, CanDeleteGroups,
                CanViewContracts, CanCreateContracts, CanEditContracts, CanDeleteContracts,
                CanViewShifts, CanCreateShifts, CanEditShifts, CanDeleteShifts,
                CanViewSchedule, CanEditSchedule, CanPlan,
                CanViewSettings, CanEditSettings,
                CanUseAssistant, CanEditClientNotes,
                CanManageAutomation, CanManageAccessTokens
            },
            Roles.Authorised => new[]
            {
                CanViewClients, CanCreateClients, CanEditClients,
                CanViewGroups, CanCreateGroups, CanEditGroups,
                CanViewContracts, CanCreateContracts, CanEditContracts,
                CanViewShifts, CanCreateShifts, CanEditShifts,
                CanViewSchedule, CanEditSchedule, CanPlan,
                CanUseAssistant, CanEditClientNotes,
                CanManageAutomation, CanManageAccessTokens
            },
            // Also the fallback for any unrecognised role string, deliberately: an unknown role gets
            // the same Planer floor as a caller without any role at all, so a future or misspelled
            // role name can never silently produce a user with no rights.
            _ => PlannerFloorPermissions
        };
    }

    /// <summary>
    /// Turns role names into the rights list every skill path expects: the role names themselves —
    /// the Admin bypass in the skill executor matches on the role string, not on a granular right —
    /// followed by the granular permissions each role expands to, without duplicates. A caller
    /// without any role is not a caller without any rights: an empty (or blank-only) role list
    /// yields the Planer floor, with no role name in front of it.
    /// </summary>
    /// <param name="roles">Role names, typically the role claims of the caller</param>
    public static List<string> ExpandRoles(IEnumerable<string> roles)
    {
        var rights = new List<string>();

        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            if (!rights.Contains(role))
            {
                rights.Add(role);
            }

            foreach (var permission in GetPermissionsForRole(role))
            {
                if (!rights.Contains(permission))
                {
                    rights.Add(permission);
                }
            }
        }

        if (rights.Count == 0)
        {
            rights.AddRange(PlannerFloorPermissions);
        }

        return rights;
    }

    public static bool HasPermission(IReadOnlyList<string> userPermissions, string requiredPermission)
    {
        return userPermissions.Contains(requiredPermission) ||
               userPermissions.Contains(Roles.Admin);
    }

    public static bool HasAnyPermission(IReadOnlyList<string> userPermissions, params string[] requiredPermissions)
    {
        return userPermissions.Contains(Roles.Admin) ||
               requiredPermissions.Any(p => userPermissions.Contains(p));
    }

    public static bool HasAllPermissions(IReadOnlyList<string> userPermissions, params string[] requiredPermissions)
    {
        return userPermissions.Contains(Roles.Admin) ||
               requiredPermissions.All(p => userPermissions.Contains(p));
    }

    public static bool HasAllRequiredPermissions(IReadOnlyList<string> userPermissions, string? requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            return true;
        }

        var required = requiredPermission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return HasAllPermissions(userPermissions, required);
    }
}
