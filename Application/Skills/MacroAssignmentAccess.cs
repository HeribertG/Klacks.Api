// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The explicit administrator check of the macro assignment skills and their confirmation preview. CanEditSettings alone
/// is not enough (owner decision 5): switching the payroll formula of a live shift or absence type is reserved for the
/// Admin role itself, whose name Permissions.ExpandRoles puts in front of the caller's expanded rights.
/// </summary>
/// <param name="context">The skill execution context of the caller</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public static class MacroAssignmentAccess
{
    public const string AdminOnlyMessage =
        "Switching the calculation macro of a shift or an absence type, and undoing such a switch, is reserved for "
        + "administrators. Nothing was changed.";

    public static bool IsAdmin(SkillExecutionContext context) => context.UserPermissions.Contains(Roles.Admin);
}
