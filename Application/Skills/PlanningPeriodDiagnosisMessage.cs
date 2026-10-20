// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// English sentences of the planning-period diagnosis, composed from a PlanningPeriodDiagnosis: consecutive days with
/// the same findings are merged into one span, each finding names what the schedule shows (dark for a day outside the
/// employment, blue and locked for a day outside a group membership, a lock for a sealed day) and the step that lifts it.
/// The model relays the sentences in the user's language; the remedies are the ones of the booking diagnosis. Group and
/// person names pass through SkillMessageText.Name.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Skills;

internal static class PlanningPeriodDiagnosisMessage
{
    private const string SpanToken = "{span}";
    private const string PersonToken = "{person}";
    private const string GroupToken = "{group}";
    private const string FromToken = "{from}";
    private const string UntilToken = "{until}";

    private const string OutsideEmployment =
        "{span} is outside the employment of {person} ({from} to {until}); the schedule shows the day dark (not employed) " +
        "and nothing can be planned on it.";

    private const string OutsideEmploymentAndGroup =
        "{span} is outside the employment of {person} ({from} to {until}); nothing can be planned on it. Because the day " +
        "is also outside the group membership, the schedule shows it blue (the blue marking covers the dark one).";

    private const string OutsideGroup =
        "{span} is outside the period in which {person} belongs to group '{group}' ({from} to {until}); the schedule of " +
        "that group shows the day blue and locked.";

    private const string OutsideCombinedGroups =
        "{span} is outside the combined membership period of {person} in the groups {groups} ({from} to {until}); the " +
        "schedule shows the day blue and locked. The single periods are: {periods}.";

    private const string GroupsToken = "{groups}";
    private const string PeriodsToken = "{periods}";
    private const string PeriodSeparator = "; ";
    private const string NameSeparator = ", ";

    private const string DayApproved = "{span} has been approved (day approval) for {person}; approved days are fixed.";

    private const string PeriodClosed = "{span} lies in a closed period for {person}; closed days are fixed.";

    private const string AllClear =
        "Nothing in the employment period, the group membership periods or the day seals stops planning {person} on " +
        "{span}. Existing entries, qualifications and working-time rules were not part of this check.";

    private const string NotMemberOfGroup =
        "{person} has no membership in group '{group}' at all, so the person does not appear in the schedule of that group.";

    private const string NoGroup = "{person} belongs to no group that the caller may see.";

    private const string HiddenGroups =
        "{count} further group membership(s) lie outside the caller's group scope and are not named.";

    private const string NoEmploymentRecord =
        "{person} has no employment record; without one the schedule does not lock any day for the employment.";

    private const string Verdict = "Not plannable on {blocked} of {total} day(s).";

    private const string CountToken = "{count}";
    private const string BlockedToken = "{blocked}";
    private const string TotalToken = "{total}";
    private const string RangeSeparator = " to ";
    private const string OpenEnd = "open";
    private const string Joiner = " ";

    internal static string Compose(PlanningPeriodDiagnosis diagnosis)
    {
        var person = SkillMessageText.Name(diagnosis.Person) ?? string.Empty;
        var days = diagnosis.Days;
        var rangeText = days.Count == 1 ? Iso(days[0].Date) : Iso(days[0].Date) + RangeSeparator + Iso(days[^1].Date);
        var blocked = days.Count(day => day.HasFinding);
        var builder = new StringBuilder();
        builder.Append(person).Append(", ").Append(rangeText).Append(": ");

        if (blocked == 0)
        {
            builder.Append(AllClear.Replace(PersonToken, person).Replace(SpanToken, rangeText));
        }
        else
        {
            builder.Append(Verdict
                .Replace(BlockedToken, blocked.ToString(CultureInfo.InvariantCulture))
                .Replace(TotalToken, days.Count.ToString(CultureInfo.InvariantCulture)));
            foreach (var sentence in Spans(diagnosis, person))
            {
                builder.Append(Joiner).Append(sentence);
            }
        }

        foreach (var note in Notes(diagnosis, person))
        {
            builder.Append(Joiner).Append(note);
        }

        builder.Append(Joiner).Append(WorkBookingDiagnosisTexts.RelayInstruction);
        return builder.ToString();
    }

    private static IEnumerable<string> Notes(PlanningPeriodDiagnosis diagnosis, string person)
    {
        if (diagnosis.Employment is null)
        {
            yield return NoEmploymentRecord.Replace(PersonToken, person);
        }

        if (diagnosis.NotMemberOfRequestedGroup)
        {
            yield return NotMemberOfGroup.Replace(PersonToken, person)
                .Replace(GroupToken, SkillMessageText.Name(diagnosis.RequestedGroupName) ?? string.Empty);
        }
        else if (diagnosis.Groups.Count == 0 && diagnosis.RequestedGroupName is null)
        {
            yield return NoGroup.Replace(PersonToken, person);
        }

        if (diagnosis.HiddenGroupMembershipCount > 0)
        {
            yield return HiddenGroups.Replace(
                CountToken, diagnosis.HiddenGroupMembershipCount.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static IEnumerable<string> Spans(PlanningPeriodDiagnosis diagnosis, string person)
    {
        var index = 0;
        var days = diagnosis.Days;
        while (index < days.Count)
        {
            var first = days[index];
            if (!first.HasFinding)
            {
                index++;
                continue;
            }

            var last = index;
            while (last + 1 < days.Count && SameFindings(first, days[last + 1]))
            {
                last++;
            }

            var span = last == index ? Iso(first.Date) : Iso(first.Date) + RangeSeparator + Iso(days[last].Date);
            foreach (var sentence in Sentences(diagnosis, first, span, person))
            {
                yield return sentence;
            }

            index = last + 1;
        }
    }

    private static IEnumerable<string> Sentences(
        PlanningPeriodDiagnosis diagnosis, PlanningDayStatus day, string span, string person)
    {
        if (day.OutsideEmployment && diagnosis.Employment is { } employment)
        {
            yield return (day.OutsideGroupPeriod ? OutsideEmploymentAndGroup : OutsideEmployment)
                .Replace(SpanToken, span).Replace(PersonToken, person)
                .Replace(FromToken, Iso(employment.ValidFrom)).Replace(UntilToken, IsoOrOpen(employment.ValidUntil))
                + Joiner + WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.AdjustMembership];
        }

        if (day.OutsideGroupPeriod)
        {
            yield return GroupSentence(diagnosis, span, person) + Joiner
                + WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.AdjustGroupMembership];
        }

        if (day.Seal == WorkBookingSealKind.DayApproval)
        {
            yield return DayApproved.Replace(SpanToken, span).Replace(PersonToken, person)
                + Joiner + WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.RevokeDayApproval];
        }
        else if (day.Seal == WorkBookingSealKind.PeriodClosed)
        {
            yield return PeriodClosed.Replace(SpanToken, span).Replace(PersonToken, person)
                + Joiner + WorkBookingDiagnosisTexts.Remedies[WorkBookingRemedy.ReopenPeriodAdmin];
        }
    }

    private static string GroupSentence(PlanningPeriodDiagnosis diagnosis, string span, string person)
    {
        var from = IsoOrOpen(diagnosis.CombinedGroupFrom);
        var until = IsoOrOpen(diagnosis.CombinedGroupUntil);
        if (diagnosis.Groups.Count == 1)
        {
            return OutsideGroup.Replace(SpanToken, span).Replace(PersonToken, person)
                .Replace(GroupToken, SkillMessageText.Name(diagnosis.Groups[0].GroupName) ?? string.Empty)
                .Replace(FromToken, from).Replace(UntilToken, until);
        }

        var names = string.Join(NameSeparator, diagnosis.Groups.Select(g => SkillMessageText.Name(g.GroupName)));
        var periods = string.Join(
            PeriodSeparator,
            diagnosis.Groups.Select(g =>
                $"{SkillMessageText.Name(g.GroupName)} ({IsoOrOpen(g.From)}{RangeSeparator}{IsoOrOpen(g.Until)})"));
        return OutsideCombinedGroups.Replace(SpanToken, span).Replace(PersonToken, person)
            .Replace(GroupsToken, names).Replace(FromToken, from).Replace(UntilToken, until).Replace(PeriodsToken, periods);
    }

    private static bool SameFindings(PlanningDayStatus left, PlanningDayStatus right) =>
        right.HasFinding
        && left.OutsideEmployment == right.OutsideEmployment
        && left.Seal == right.Seal
        && left.OutsideGroupPeriod == right.OutsideGroupPeriod;

    private static string Iso(DateOnly date) => date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture);

    private static string IsoOrOpen(DateOnly? date) => date.HasValue ? Iso(date.Value) : OpenEnd;
}
