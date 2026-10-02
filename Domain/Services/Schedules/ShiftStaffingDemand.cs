// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single definition of how many employees a shift needs per day, shared by the wizard slot expansion, the grouping
/// capacity check, the unstaffed predicate and the coverage statistics. A regular shift needs Quantity (shifts per
/// day) times SumEmployees (employees per shift); values below one count as one, mirroring the input minimum and the
/// sporadic capacity rule. A sporadic shift (Quantity = days per period) and a shift day covered by a container
/// template carry no fixed daily demand.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ShiftStaffingDemand
{
    private const int MinimumFactor = 1;
    private const int NoFixedDailyDemand = 0;

    /// <summary>
    /// Employees a regular shift needs on one day.
    /// </summary>
    /// <param name="quantity">Shifts per day; values below one count as one.</param>
    /// <param name="sumEmployees">Employees per shift; values below one count as one.</param>
    public static int PerDay(int quantity, int sumEmployees) => AtLeastOne(quantity) * AtLeastOne(sumEmployees);

    /// <summary>
    /// Employees the shift day must have engaged; zero when the day has no fixed daily demand.
    /// </summary>
    /// <param name="assignment">The shift-day row of the shift schedule.</param>
    public static int RequiredOn(ShiftDayAssignment assignment) =>
        assignment.IsSporadic || assignment.IsInTemplateContainer
            ? NoFixedDailyDemand
            : PerDay(assignment.Quantity, assignment.SumEmployees);

    private static int AtLeastOne(int value) => value < MinimumFactor ? MinimumFactor : value;
}
