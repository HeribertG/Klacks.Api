// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Group-level answer to "why are these cells dark or blue": how many of the employees the schedule shows for the group
/// have at least one checked day outside the employment (dark) and outside the combined period of their group membership
/// (blue), and when the memberships that start late begin. The counts are per employee, not per day.
/// </summary>
/// <param name="GroupName">Display name of the group</param>
/// <param name="From">First checked day</param>
/// <param name="Until">Last checked day</param>
/// <param name="PersonCount">Employees the schedule shows for the group on the checked days</param>
/// <param name="OutsideEmploymentCount">Employees with a checked day outside their employment</param>
/// <param name="OutsideGroupPeriodCount">Employees with a checked day outside the combined period of their group memberships</param>
/// <param name="LateGroupStartCount">Employees whose group membership starts after the first checked day</param>
/// <param name="EarlyGroupEndCount">Employees whose group membership ends before the last checked day</param>
/// <param name="EarliestLateGroupStart">Earliest start among the late-starting memberships, null when there is none</param>
/// <param name="MostCommonLateGroupStart">Start date shared by most late-starting memberships (earlier date on a tie), null when there is none</param>
/// <param name="MostCommonLateGroupStartCount">Number of employees that share that start date</param>
/// <param name="SealedDays">Dates with at least one sealed employee, oldest first</param>
public sealed record PlanningGroupSummary(
    string GroupName,
    DateOnly From,
    DateOnly Until,
    int PersonCount,
    int OutsideEmploymentCount,
    int OutsideGroupPeriodCount,
    int LateGroupStartCount,
    int EarlyGroupEndCount,
    DateOnly? EarliestLateGroupStart,
    DateOnly? MostCommonLateGroupStart,
    int MostCommonLateGroupStartCount,
    IReadOnlyList<PlanningSealedDayCount> SealedDays);
