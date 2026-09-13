// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses a user-supplied date/date-time string for a skill parameter that will be stored in a
/// 'timestamp with time zone' column. Npgsql rejects Kind=Unspecified/Local outright, so every parse
/// here forces Kind=Utc — but the written calendar day and wall clock are kept exactly as given, no
/// offset arithmetic is applied. All current callers (Contract/Membership validFrom/validUntil,
/// Address.ValidFrom) treat the result as a calendar boundary, not a precise instant: converting an
/// offset to its equivalent UTC instant would silently move a date like "2026-08-01T00:00:00+02:00"
/// to the previous day. The parsing policy itself lives in SkillCalendarStringParser: ISO 8601 first
/// with InvariantCulture, then the cultures of the user's language. The overload without a language
/// keeps the historical culture list (Swiss/German dotted dates first). Note this is NOT the same
/// acceptance set as SkillParameterTypeValidator's dispatch-time gate, which additionally accepts
/// "today" words (e.g. "heute") that this parser rejects — see SkillDateParser for that case.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillUtcDateTimeParser
{
    public static bool TryParse(string? raw, out DateTime value) => TryParse(raw, null, out value);

    /// <param name="raw">Value as the user or the LLM wrote it</param>
    /// <param name="language">UI language of the user the value came from; null keeps the default cultures</param>
    /// <param name="value">Parsed value with Kind=Utc and the written day and wall clock unchanged</param>
    public static bool TryParse(string? raw, string? language, out DateTime value) =>
        SkillCalendarStringParser.TryParseDateTime(raw, language, out value);
}
