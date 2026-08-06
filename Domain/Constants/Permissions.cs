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
                CanUseAssistant
            },
            Roles.Authorised => new[]
            {
                CanViewClients, CanCreateClients, CanEditClients,
                CanViewGroups, CanCreateGroups, CanEditGroups,
                CanViewContracts, CanCreateContracts, CanEditContracts,
                CanViewShifts, CanCreateShifts, CanEditShifts,
                CanViewSchedule, CanEditSchedule, CanPlan,
                CanUseAssistant
            },
            _ => new[] { CanViewClients, CanViewGroups, CanViewSchedule }
        };
    }

    /// <summary>
    /// Turns role names into the rights list every skill path expects: the role names themselves —
    /// the Admin bypass in the skill executor matches on the role string, not on a granular right —
    /// followed by the granular permissions each role expands to, without duplicates.
    /// </summary>
    /// <param name="roles">Role names, typically the role claims of the caller</param>
    public static List<string> ExpandRoles(IEnumerable<string> roles)
    {
        var rights = new List<string>();

        foreach (var role in roles)
        {
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
