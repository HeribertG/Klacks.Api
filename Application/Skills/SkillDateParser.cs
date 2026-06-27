// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for skills that accept an optional user-supplied date (e.g. a group membership's
/// ValidFrom — the plannability boundary in the schedule). Distinguishes three cases so a present but
/// unreadable date is never silently treated as "now": absent/blank (caller may default), a recognised
/// date or "today" word, and present-but-unparseable (the caller should ask the user for a concrete date).
/// </summary>

using System.Globalization;

namespace Klacks.Api.Application.Skills;

internal static class SkillDateParser
{
    private static readonly string[] TodayWords =
    {
        "today", "heute", "now", "jetzt", "sofort", "ab sofort", "ab heute",
        "aujourd'hui", "oggi"
    };

    private static readonly CultureInfo[] Cultures =
    {
        new("de-CH"), new("de-DE"), new("fr-CH"), new("it-CH"),
        CultureInfo.InvariantCulture, new("en-US")
    };

    /// <summary>
    /// Clarification a membership skill returns when a non-blank start date was supplied but could not
    /// be understood, so the caller asks the user for a concrete date instead of defaulting to today.
    /// </summary>
    public const string InvalidDateMessage =
        "I couldn't read the start date for the membership. Please give a concrete date " +
        "(for example 2026-05-01) or say 'today'.";

    /// <summary>
    /// Parses an optional date to a UTC midnight value. Returns Invalid=true when a non-blank value
    /// was given that could not be understood, so the caller can reject it instead of defaulting.
    /// </summary>
    public static (DateTime? Value, bool Invalid) ParseOptionalUtcDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, false);
        }

        var trimmed = raw.Trim();
        if (TodayWords.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            return (DateTime.UtcNow.Date, false);
        }

        foreach (var culture in Cultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.None, out var parsed))
            {
                return (DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc), false);
            }
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
