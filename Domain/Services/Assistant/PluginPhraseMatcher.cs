// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared matching helper for plugin-language phrase lists used by the conversation-signal
/// detectors (affirmation, negation, correction, cancellation). Entries in space-segmented
/// scripts must match a whole token or, for multi-word phrases, a substring — this avoids
/// substring false positives such as "nie" inside the Polish word "niebieski". Entries in
/// non-segmented scripts (Han, Kana, Thai) are matched by substring because those languages
/// have no word boundaries to tokenize on.
/// </summary>
/// MatchesAny tests a message against plugin entries; Merge folds newly loaded plugin phrases into an
/// existing entry array (trimmed, lowercased, de-duplicated) for the detectors' Configure methods.
/// <param name="lowerMessage">The user message lowered via ToLowerInvariant</param>
/// <param name="tokens">Lowercased letter tokens extracted from the message</param>
/// <param name="entries">Plugin phrase entries, stored trimmed and lowercase</param>
/// <param name="existing">The already-loaded entries to merge into</param>
/// <param name="added">Newly loaded plugin phrases to normalize and add</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class PluginPhraseMatcher
{
    private const char ThaiRangeStart = '\u0E00';
    private const char ThaiRangeEnd = '\u0E7F';
    private const char KanaRangeStart = '\u3040';
    private const char KanaRangeEnd = '\u30FF';
    private const char HanRangeStart = '\u3400';
    private const char HanRangeEnd = '\u9FFF';
    private const char HanCompatibilityRangeStart = '\uF900';
    private const char HanCompatibilityRangeEnd = '\uFAFF';
    private const char PhraseWordSeparator = ' ';

    public static bool MatchesAny(string lowerMessage, IReadOnlyCollection<string> tokens, string[] entries)
    {
        foreach (var entry in entries)
        {
            if (tokens.Contains(entry))
            {
                return true;
            }

            var matchAsSubstring = entry.Contains(PhraseWordSeparator) || UsesNonSegmentedScript(entry);
            if (matchAsSubstring && lowerMessage.Contains(entry, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string[] Merge(string[] existing, IEnumerable<string> added)
    {
        return existing
            .Concat(added.Select(entry => entry.Trim().ToLowerInvariant()))
            .Where(entry => entry.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool UsesNonSegmentedScript(string entry)
    {
        foreach (var ch in entry)
        {
            if (ch is >= ThaiRangeStart and <= ThaiRangeEnd
                or >= KanaRangeStart and <= KanaRangeEnd
                or >= HanRangeStart and <= HanRangeEnd
                or >= HanCompatibilityRangeStart and <= HanCompatibilityRangeEnd)
            {
                return true;
            }
        }

        return false;
    }
}
