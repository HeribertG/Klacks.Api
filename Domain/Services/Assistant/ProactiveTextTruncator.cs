// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Caps free text to the width of the database column that stores it. Every proactive dispatch
/// column is a bounded varchar, and the text feeding it (a trigger summary, a skill description) is
/// unbounded, so the cap has to happen before EF hands the value to Postgres — an overlong value
/// does not truncate there, it aborts the whole insert.
/// The cut never splits a UTF-16 surrogate pair: a lone surrogate is not encodable as UTF-8 and
/// would break the value on the wire instead of merely shortening it. Postgres counts characters
/// rather than bytes for varchar, and a surrogate pair is two .NET chars but one character, so
/// measuring in .NET length always errs on the safe side.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ProactiveTextTruncator
{
    /// <summary>
    /// Returns the value unchanged when it fits, otherwise its longest prefix that leaves room for
    /// the truncation suffix and ends on a whole character.
    /// </summary>
    /// <param name="value">Text to cap; null and empty pass through untouched.</param>
    /// <param name="maxLength">Column width in characters, including the truncation suffix.</param>
    public static string? Cap(string? value, int maxLength)
    {
        if (value == null || value.Length <= maxLength)
        {
            return value;
        }

        var keepLength = maxLength - ProactiveTriggerDispatchLimits.TruncationSuffix.Length;
        if (keepLength <= 0)
        {
            return string.Empty;
        }

        if (char.IsHighSurrogate(value[keepLength - 1]))
        {
            keepLength--;
        }

        return value[..keepLength] + ProactiveTriggerDispatchLimits.TruncationSuffix;
    }
}
