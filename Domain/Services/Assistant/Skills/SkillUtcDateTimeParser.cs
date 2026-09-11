// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses a user-supplied date/date-time string for a skill parameter that will be stored in a
/// 'timestamp with time zone' column. Npgsql rejects Kind=Unspecified/Local outright, so every parse
/// here forces Kind=Utc — but the written calendar day and wall clock are kept exactly as given, no
/// offset arithmetic is applied. All current callers (Contract/Membership validFrom/validUntil,
/// Address.ValidFrom) treat the result as a calendar boundary, not a precise instant: converting an
/// offset to its equivalent UTC instant would silently move a date like "2026-08-01T00:00:00+02:00"
/// to the previous day. Tries each accepted culture (Swiss/German dotted dates included). Note this is
/// NOT the same acceptance set as SkillParameterTypeValidator's dispatch-time gate, which additionally
/// accepts "today" words (e.g. "heute") that this parser rejects — see SkillDateParser for that case.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillUtcDateTimeParser
{
    public static bool TryParse(string? raw, out DateTime value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = default;
            return false;
        }

        var trimmed = raw.Trim();
        foreach (var culture in SkillDateParsingDefaults.Cultures)
        {
            if (DateTimeOffset.TryParse(trimmed, culture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                value = DateTime.SpecifyKind(parsed.DateTime, DateTimeKind.Utc);
                return true;
            }
        }

        value = default;
        return false;
    }
}
