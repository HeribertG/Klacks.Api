// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shapes a PlanningGroupSummary into the structured data of the skill result: the counts of dark and blue employees, the
/// late group starts and the sealed days, with dates as yyyy-MM-dd text and a missing date as null.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Skills;

internal static class PlanningGroupSummaryData
{
    internal static object From(PlanningGroupSummary summary) => new
    {
        Group = summary.GroupName,
        From = Iso(summary.From),
        Until = Iso(summary.Until),
        EmployeesShown = summary.PersonCount,
        OutsideEmployment = summary.OutsideEmploymentCount,
        OutsideGroupPeriod = summary.OutsideGroupPeriodCount,
        LateGroupStart = new
        {
            Employees = summary.LateGroupStartCount,
            MostCommon = IsoOrNull(summary.MostCommonLateGroupStart),
            MostCommonEmployees = summary.MostCommonLateGroupStartCount,
            Earliest = IsoOrNull(summary.EarliestLateGroupStart),
        },
        EarlyGroupEndEmployees = summary.EarlyGroupEndCount,
        SealedDays = summary.SealedDays
            .Select(day => new { Date = Iso(day.Date), Employees = day.PersonCount })
            .ToList(),
    };

    private static string Iso(DateOnly date) => date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture);

    private static string? IsoOrNull(DateOnly? date) => date.HasValue ? Iso(date.Value) : null;
}
