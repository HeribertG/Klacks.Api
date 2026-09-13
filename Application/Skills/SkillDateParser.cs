// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for skills that accept an optional user-supplied date (e.g. a group membership's
/// ValidFrom — the plannability boundary in the schedule). Distinguishes three cases so a present but
/// unreadable date is never silently treated as "now": absent/blank (caller may default), a recognised
/// date or "today" word, and present-but-unparseable (the caller should ask the user for a concrete date).
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

internal static class SkillDateParser
{
    /// <summary>
    /// Clarification naming the parameter that could not be read, so the model knows which of several
    /// date arguments to ask about instead of retrying the whole call blindly.
    /// </summary>
    /// <param name="parameterName">Declared parameter name, e.g. "validFrom".</param>
    /// <param name="raw">The value as the user or the model wrote it, quoted back to the model.</param>
    public static string InvalidDateMessageFor(string parameterName, string raw) =>
        $"Invalid {parameterName} value: '{raw}'. Please give a concrete date (for example 2026-05-01), " +
        "a relative day such as 'today' or 'tomorrow', or ask the user which date is meant.";

    /// <summary>
    /// Clarification for a birthdate that could not be read. A birthdate is a fixed historical fact, so
    /// relative day words are deliberately rejected here rather than resolved against the company day.
    /// </summary>
    /// <param name="raw">The value as the user or the model wrote it, quoted back to the model.</param>
    public static string InvalidBirthdateMessage(string raw) =>
        $"Invalid birthdate '{raw}'. Please give a concrete date (for example 1984-05-01) in ISO " +
        "format; relative words such as 'today' or 'tomorrow' are not valid birthdates.";

    /// <summary>
    /// Parses an optional date to a UTC midnight value. Returns Invalid=true when a non-blank value
    /// was given that could not be understood, so the caller can reject it instead of defaulting.
    /// Delegates the actual date/time reading to <see cref="SkillUtcDateTimeParser"/> (then takes just
    /// the calendar day) so this never disagrees with it about what an offset or "Z" value resolves to.
    /// </summary>
    /// <param name="raw">The user-supplied date string (may be null/blank, a date, or a relative day
    /// word such as "today", "tomorrow" or "yesterday" in any supported language).</param>
    /// <param name="today">
    /// The company's current calendar date as a UTC-midnight value (from <c>ICompanyClock</c>), used as
    /// the anchor for relative day words so the membership date reflects the company's local day rather
    /// than the server's UTC day. Tomorrow and yesterday are that day shifted by one, keeping Kind=Utc.
    /// </param>
    /// <param name="language">UI language of the calling user, so an ambiguous written date such as
    /// "03/04/2026" is read the way that user writes it and a relative day word is only recognised in
    /// that language or in English; null keeps the historical culture list and the full word union.</param>
    public static (DateTime? Value, bool Invalid) ParseOptionalUtcDate(
        string? raw, DateTime today, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, false);
        }

        var trimmed = raw.Trim();
        if (SkillRelativeDayWords.TryResolveDayOffset(trimmed, language, out var dayOffset))
        {
            return (today.AddDays(dayOffset), false);
        }

        if (SkillUtcDateTimeParser.TryParse(trimmed, language, out var parsed))
        {
            return (parsed.Date, false);
        }

        return (null, true);
    }

    /// <summary>
    /// Builds the clarification a membership skill returns when no start date was supplied and it must
    /// obtain one from the user instead of silently defaulting to today. The wording tells the model to
    /// ask, not invent — a fabricated date cannot be caught downstream, only an absent one.
    /// </summary>
    /// <param name="action">What the skill is about to do, e.g. "add the selected client(s) to group 'Bern'".</param>
    public static string MissingStartDateMessage(string action) =>
        $"Before I {action}, I need to know from which date the membership should be valid. " +
        "Ask the user for a concrete start date (for example 2026-05-01) or 'today' — do not assume or " +
        "invent a date yourself — then call again with that validFrom.";

    /// <summary>
    /// Suffix for a read-only preview message when no start date was supplied yet, so the model collects
    /// it in the same confirmation turn instead of after the user has already said "yes".
    /// </summary>
    public const string AskForStartDateInPreview =
        " Also ask the user from which date the membership should start (for example 2026-05-01 or " +
        "'today') and pass it as validFrom when applying — do not invent a date.";
}
