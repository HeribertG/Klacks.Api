// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What stops planning an employee on one day: the day lies outside the employment (the grid shows it dark), outside
/// the combined period of the employee's group memberships (the grid shows it blue and locked, because it spans the
/// earliest start to the latest end of all items of the shown groups), or the day is sealed. The groups whose own period
/// does not cover the day are listed for information; they do not make the day blue on their own.
/// </summary>
/// <param name="Date">The checked day</param>
/// <param name="OutsideEmployment">True when the employee has an employment record that does not cover the day</param>
/// <param name="OutsideGroupPeriod">True when the employee has group memberships and the day lies outside their combined range</param>
/// <param name="OutsideSingleGroupNames">Names of the groups whose own membership period does not cover the day, ordered by name</param>
/// <param name="Seal">Kind of the seal covering the day, null when the day is not sealed</param>
public sealed record PlanningDayStatus(
    DateOnly Date,
    bool OutsideEmployment,
    bool OutsideGroupPeriod,
    IReadOnlyList<string> OutsideSingleGroupNames,
    WorkBookingSealKind? Seal)
{
    public bool HasFinding => OutsideEmployment || OutsideGroupPeriod || Seal.HasValue;
}
