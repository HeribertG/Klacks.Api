// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// English sentences of the group-level planning-period summary, composed from a PlanningGroupSummary: how many employees the
/// schedule shows dark (outside the employment) and blue and locked (outside the group membership), when the late memberships
/// start, which days are sealed, and the step that lifts each finding. The model relays the sentences in the user's language;
/// the remedies are the ones of the booking diagnosis. The group name passes through SkillMessageText.Name.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Skills;

internal static class PlanningGroupSummaryMessage
{
    private const string GroupToken = "{group}";
    private const string TotalToken = "{total}";
    private const string CountToken = "{count}";
    private const string FromToken = "{from}";
    private const string UntilToken = "{until}";
    private const string DateToken = "{date}";
    private const string EarliestToken = "{earliest}";
    private const string Joiner = " ";
    private const string RangeSeparator = " to ";
    private const string ListSeparator = "; ";

    private const string NoEmployees =
        "Group '{group}' has no employee whose employment overlaps these days, so there is nothing to summarize.";

    private const string Shown = "The schedule of group '{group}' shows {total} employee(s) on these days.";

    private const string DarkFinding =
        "{count} of {total} employee(s) have days outside their employment; the schedule shows those days dark (not " +
        "employed), or blue where the group membership does not apply either, and nothing can be planned on them.";

    private const string NoDark = "No employee has a day outside the employment.";

    private const string BlueFinding =
        "{count} of {total} employee(s) have days outside the period of their membership in this group; the schedule " +
        "shows those days blue and locked.";

    private const string LateStart =
        "For {late} of them the membership starts after {from}: the most common start is {date} ({shared} employee(s)), " +
        "the earliest is {earliest}.";

    private const string EarlyEnd = "{count} employee(s) have a membership that ends before {until}.";

    private const string NoBlue = "No employee has a day outside the group membership period.";

    private const string GroupLevelRemedy =
        "A start after the first day is usually the date the employee was assigned to the group; moving it back to the " +
        "employee's entry date, one employee at a time in the assigned groups of the employee (valid from) or for several " +
        "at once by assigning them to the group again with the start set to the entry date, turns those days normal.";

    private const string SealedFinding = "Sealed days (day approval or closed period): {list}.";

    private const string NoSealed = "No day of the range is sealed for these employees.";

    private const string CountedScope =
        "Employees whose employment does not overlap these days at all are not rows of the schedule and are not counted.";

    private const string ListToken = "{list}";
    private const string LateToken = "{late}";
    private const string SharedToken = "{shared}";
    private const string SealedEntry = "{date} ({count} employee(s))";

    internal static string Compose(PlanningGroupSummary summary)
    {
        var group = SkillMessageText.Name(summary.GroupName) ?? string.Empty;
        var builder = new StringBuilder();
        builder.Append(group).Append(", ").Append(Iso(summary.From)).Append(RangeSeparator).Append(Iso(summary.Until)).Append(": ");

        if (summary.PersonCount == 0)
        {
            builder.Append(NoEmployees.Replace(GroupToken, group));
        }
        else
        {
            builder.Append(Shown.Replace(GroupToken, group).Replace(TotalToken, Number(summary.PersonCount)));
            Append(builder, Dark(summary));
            Append(builder, Blue(summary));
            Append(builder, Sealed(summary));
            Append(builder, CountedScope);
        }

        builder.Append(Joiner).Append(WorkBookingDiagnosisTexts.RelayInstruction);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string sentence) => builder.Append(Joiner).Append(sentence);

    private static string Dark(PlanningGroupSummary summary) => summary.OutsideEmploymentCount == 0
        ? NoDark
        : DarkFinding.Replace(CountToken, Number(summary.OutsideEmploymentCount)).Replace(TotalToken, Number(summary.PersonCount))
            + Joiner + WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.AdjustMembership];

    private static string Blue(PlanningGroupSummary summary)
    {
        if (summary.OutsideGroupPeriodCount == 0)
        {
            return NoBlue;
        }

        var builder = new StringBuilder(BlueFinding
            .Replace(CountToken, Number(summary.OutsideGroupPeriodCount))
            .Replace(TotalToken, Number(summary.PersonCount)));
        if (summary.LateGroupStartCount > 0)
        {
            builder.Append(Joiner).Append(LateStart
                .Replace(LateToken, Number(summary.LateGroupStartCount))
                .Replace(FromToken, Iso(summary.From))
                .Replace(DateToken, IsoOrOpen(summary.MostCommonLateGroupStart))
                .Replace(SharedToken, Number(summary.MostCommonLateGroupStartCount))
                .Replace(EarliestToken, IsoOrOpen(summary.EarliestLateGroupStart)));
        }

        if (summary.EarlyGroupEndCount > 0)
        {
            builder.Append(Joiner).Append(EarlyEnd
                .Replace(CountToken, Number(summary.EarlyGroupEndCount))
                .Replace(UntilToken, Iso(summary.Until)));
        }

        builder.Append(Joiner).Append(WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.AdjustGroupMembership]);
        if (summary.LateGroupStartCount > 0)
        {
            builder.Append(Joiner).Append(GroupLevelRemedy);
        }

        return builder.ToString();
    }

    private static string Sealed(PlanningGroupSummary summary) => summary.SealedDays.Count == 0
        ? NoSealed
        : SealedFinding.Replace(
            ListToken,
            string.Join(
                ListSeparator,
                summary.SealedDays.Select(day => SealedEntry
                    .Replace(DateToken, Iso(day.Date))
                    .Replace(CountToken, Number(day.PersonCount)))));

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Iso(DateOnly date) => date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture);

    private static string IsoOrOpen(DateOnly? date) => date.HasValue ? Iso(date.Value) : SkillDisplayFormats.OpenEnd;
}
