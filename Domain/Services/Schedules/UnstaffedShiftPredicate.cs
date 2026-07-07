// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared definition of "unstaffed" for a shift-day assignment, so the proactive assistant
/// trigger (UnstaffedShift7dDetector) and the read-only bot query use the exact same formula
/// instead of two independently maintained copies.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class UnstaffedShiftPredicate
{
    /// <summary>
    /// Returns true when the assignment requires staff (Quantity greater than zero) and the
    /// number of assigned employees is below that requirement.
    /// </summary>
    /// <param name="assignment">The shift-day assignment to evaluate.</param>
    public static bool IsUnstaffed(ShiftDayAssignment assignment)
    {
        return assignment.Quantity > 0 && assignment.SumEmployees < assignment.Quantity;
    }
}
