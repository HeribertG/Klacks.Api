// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the deterministic start-of-membership sentences that every client-grouping skill appends to its preview and
/// its result: the start date, that the schedule shows the days before it blue and locked for these persons, how many
/// of them entered the company earlier, and (in a preview) the question whether the memberships should rather start on
/// each person's own entry date. Deliberately English sentences and no translation key - a skill message is tool output
/// that the model renders in the user's language (the language directive is the first line of the system prompt), as
/// FirstGroupVisibilityAdvisory does. The facts themselves also travel as typed fields (GroupMembershipStartInfo), so the
/// model cannot drop them. Kept in one place so every skill, preview and result says the same thing.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class GroupMembershipStartHint
{
    private const string TellTheUser = " Tell the user this.";

    private const string FixedGivenPreview =
        "Membership start: the new memberships will start on {0}; in the schedule the days before {0} are shown blue and " +
        "locked for these persons (outside the group period).";

    private const string FixedGivenApplied =
        "Membership start: the new memberships start on {0}; in the schedule the days before {0} are shown blue and " +
        "locked for these persons (outside the group period).";

    private const string FixedNotGivenPreview =
        "Membership start: no start date was given yet; the default start date {0} would apply, and the days before it " +
        "are shown blue and locked in the schedule for these persons (outside the group period).";

    private const string FixedNotGivenApplied =
        "Membership start: no start date was given, so the new memberships start on the default date {0}; in the " +
        "schedule the days before it are shown blue and locked for these persons (outside the group period).";

    private const string EnteredEarlierPreview =
        " {0} of {1} person(s) entered the company earlier (earliest entry {2}); their days between entry and {3} would " +
        "be blue and locked.";

    private const string EnteredEarlierApplied =
        " {0} of {1} person(s) entered the company earlier (earliest entry {2}); their days between entry and {3} are " +
        "blue and locked.";

    private const string PreviewQuestion =
        " Ask the user whether the memberships should start on {0} or on each person's own entry date (pass " +
        "validFrom='{1}' when applying), and do not apply before the user has answered.";

    private const string EntryModePreview =
        "Membership start: the new memberships will start on each person's own employment entry date (earliest {0}, " +
        "latest {1}), so no employed day before the group start is shown blue and locked because of this.";

    private const string EntryModeApplied =
        "Membership start: the new memberships start on each person's own employment entry date (earliest {0}, latest " +
        "{1}), so no employed day before the group start is shown blue and locked because of this.";

    private const string WithoutEntryPreview =
        " {0} person(s) have no entry date and would start on the company's current day.";

    private const string WithoutEntryApplied =
        " {0} person(s) have no entry date and start on the company's current day.";

    private const string ExistingMembersLeftUnchanged =
        "{0} existing member(s) already hold a membership that starts after their employment entry (earliest entry {1}); " +
        "this run does not change existing memberships, so their days between entry and that start stay blue and locked.";

    internal static string For(GroupMembershipStartInfo? info, bool applied)
    {
        if (info is null)
        {
            return string.Empty;
        }

        if (info.PersonCount == 0)
        {
            return ExistingOnly(info, applied);
        }

        var text = info.Mode == GroupMembershipStartMode.EntryDate
            ? ForEntryDate(info, applied)
            : ForFixed(info, applied);
        return text;
    }

    private static string ExistingOnly(GroupMembershipStartInfo info, bool applied)
    {
        if (info.ExistingMembersStartingAfterEntryCount == 0)
        {
            return string.Empty;
        }

        var text = ExistingSentence(info);
        return applied ? text + TellTheUser : text;
    }

    private static string ExistingSentence(GroupMembershipStartInfo info) => string.Format(
        CultureInfo.InvariantCulture,
        ExistingMembersLeftUnchanged,
        info.ExistingMembersStartingAfterEntryCount,
        info.ExistingMembersEarliestEntryDate);

    internal static string ForSingle(GroupMembershipStartInfo info)
    {
        var worthSaying = info.PersonsEnteredBeforeStartCount > 0
            || info.Mode == GroupMembershipStartMode.EntryDate;
        return worthSaying ? For(info, applied: true) : string.Empty;
    }

    private static string ForFixed(GroupMembershipStartInfo info, bool applied)
    {
        var start = info.StartDate ?? string.Empty;
        var template = (info.StartDateGiven, applied) switch
        {
            (true, false) => FixedGivenPreview,
            (true, true) => FixedGivenApplied,
            (false, false) => FixedNotGivenPreview,
            (false, true) => FixedNotGivenApplied
        };
        var text = string.Format(CultureInfo.InvariantCulture, template, start);

        if (info.PersonsEnteredBeforeStartCount > 0)
        {
            text += string.Format(
                CultureInfo.InvariantCulture,
                applied ? EnteredEarlierApplied : EnteredEarlierPreview,
                info.PersonsEnteredBeforeStartCount,
                info.PersonCount,
                info.EarliestEntryDate,
                start);
            if (!applied)
            {
                text += string.Format(
                    CultureInfo.InvariantCulture, PreviewQuestion, start, SkillDateParser.EntryDateKeyword);
            }
        }

        return applied ? text + TellTheUser : text;
    }

    private static string ForEntryDate(GroupMembershipStartInfo info, bool applied)
    {
        var text = string.Format(
            CultureInfo.InvariantCulture,
            applied ? EntryModeApplied : EntryModePreview,
            info.EarliestStartDate,
            info.LatestStartDate);

        if (info.PersonsWithoutEntryDateCount > 0)
        {
            text += string.Format(
                CultureInfo.InvariantCulture,
                applied ? WithoutEntryApplied : WithoutEntryPreview,
                info.PersonsWithoutEntryDateCount);
        }

        if (info.ExistingMembersStartingAfterEntryCount > 0)
        {
            text += " " + ExistingSentence(info);
        }

        return applied ? text + TellTheUser : text;
    }
}
