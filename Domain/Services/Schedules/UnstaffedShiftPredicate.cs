// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared definition of "unstaffed" for a shift-day assignment, so the proactive assistant
/// trigger (UnstaffedShiftPeriodDetector) and the read-only bot query use the exact same formula
/// instead of two independently maintained copies. The demand comes from ShiftStaffingDemand.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class UnstaffedShiftPredicate
{
    /// <summary>
    /// Returns true when the shift day has a fixed daily demand (Quantity x SumEmployees) and fewer
    /// employees are engaged. Sporadic and container-template-covered shift days are never unstaffed.
    /// </summary>
    /// <param name="assignment">The shift-day assignment to evaluate.</param>
    public static bool IsUnstaffed(ShiftDayAssignment assignment)
    {
        var required = ShiftStaffingDemand.RequiredOn(assignment);
        return required > 0 && assignment.Engaged < required;
    }
}
