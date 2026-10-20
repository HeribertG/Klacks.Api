// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shapes a PlanningPeriodDiagnosis into the structured data of the skill result: the employment window, the group
/// membership periods and only the days that carry a finding, with dates as yyyy-MM-dd text and an open end as "open".
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class PlanningPeriodDiagnosisData
{
    private const string DarkDisplay = "dark";
    private const string BlueDisplay = "blue";
    private const string NormalDisplay = "normal";
    private const string DayApprovalSeal = "dayApproval";
    private const string PeriodClosedSeal = "periodClosed";

    internal static object From(PlanningPeriodDiagnosis diagnosis) => new
    {
        Employee = diagnosis.Person,
        Employment = diagnosis.Employment is null
            ? null
            : new { From = Iso(diagnosis.Employment.ValidFrom), Until = IsoOrOpen(diagnosis.Employment.ValidUntil) },
        HasEmploymentRecord = diagnosis.Employment is not null,
        Groups = diagnosis.Groups
            .Select(group => new { Group = group.GroupName, From = IsoOrOpen(group.From), Until = IsoOrOpen(group.Until) })
            .ToList(),
        CombinedGroupPeriod = diagnosis.Groups.Count == 0
            ? null
            : new { From = IsoOrOpen(diagnosis.CombinedGroupFrom), Until = IsoOrOpen(diagnosis.CombinedGroupUntil) },
        HiddenGroupMembershipCount = diagnosis.HiddenGroupMembershipCount,
        RequestedGroup = diagnosis.RequestedGroupName,
        NotMemberOfRequestedGroup = diagnosis.NotMemberOfRequestedGroup,
        DaysChecked = diagnosis.Days.Count,
        DaysWithFindings = diagnosis.Days.Count(day => day.HasFinding),
        Findings = diagnosis.Days.Where(day => day.HasFinding).Select(Day).ToList(),
    };

    private static object Day(PlanningDayStatus day) => new
    {
        Date = Iso(day.Date),
        OutsideEmployment = day.OutsideEmployment,
        OutsideGroupPeriod = day.OutsideGroupPeriod,
        OutsideSingleGroups = day.OutsideSingleGroupNames,
        Seal = day.Seal switch
        {
            WorkBookingSealKind.DayApproval => DayApprovalSeal,
            WorkBookingSealKind.PeriodClosed => PeriodClosedSeal,
            _ => null
        },
        ScheduleShows = day.OutsideGroupPeriod ? BlueDisplay : day.OutsideEmployment ? DarkDisplay : NormalDisplay,
    };

    private static string Iso(DateOnly date) => date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture);

    private static string IsoOrOpen(DateOnly? date) => date.HasValue ? Iso(date.Value) : SkillDisplayFormats.OpenEnd;
}
