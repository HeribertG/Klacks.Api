// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

using System.Globalization;
using System.Text;

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
    private const char HangulJamoStart = '\u1100';
    private const char HangulJamoEnd = '\u11FF';
    private const char HangulCompatibilityJamoStart = '\u3130';
    private const char HangulCompatibilityJamoEnd = '\u318F';
    private const char HangulSyllablesStart = '\uAC00';
    private const char HangulSyllablesEnd = '\uD7AF';
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

    /// <summary>
    /// Composed (NFC) form of the text, so a message that arrives decomposed still matches the composed vocabulary
    /// entries. Invalid surrogate input is passed through rather than thrown at: this runs on every chat turn, and a
    /// malformed message must cost a detection, not the turn.
    /// </summary>
    internal static string ComposeNfc(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            return value.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return value;
        }
    }

    public static string[] Merge(string[] existing, IEnumerable<string> added)
    {
        return existing
            .Concat(added.Select(entry => entry.Trim().ToLowerInvariant()))
            .Where(entry => entry.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// True when the message opens with the entry AND the entry ends where a word ends. A segmented-script
    /// entry must not stop inside a longer word - the Malay refusal "tak" opens "Tak, terima kasih" but not
    /// "Take the first shift", the Spanish "no" opens "No, gracias" but not "Nombre del cliente" - while an
    /// entry in a script whose words carry attached suffixes or have no separators at all (Han, Kana, Thai and
    /// Hangul: "취소" opens "취소해줘") keeps matching as a plain prefix. A letter or a combining mark right after
    /// the entry continues the word; an entry that itself ends in punctuation needs no boundary.
    /// </summary>
    /// <param name="lowerMessage">The user message lowered via ToLowerInvariant, without leading whitespace</param>
    /// <param name="entry">Plugin phrase entry, stored trimmed and lowercase</param>
    internal static bool StartsWithEntryAtWordEnd(string lowerMessage, string entry)
    {
        if (entry.Length == 0 || !lowerMessage.StartsWith(entry, StringComparison.Ordinal))
        {
            return false;
        }

        return IsAttachedSuffixScript(entry) || EndsAtWordBoundary(lowerMessage, entry.Length);
    }

    /// <summary>
    /// The one place that decides which scripts keep prefix semantics for a phrase entry: those written without
    /// word separators (Han, Kana, Thai) and Hangul, whose words are separated by blanks but carry attached
    /// particles and verb endings (a Korean refusal entry for "cancel" opens the same word with a verb ending
    /// attached). Every other script has to end an entry where the word ends. Hangul is deliberately not part of
    /// UsesNonSegmentedScript: its words are separated by blanks, so whole-token matching stays correct for it.
    /// </summary>
    /// <param name="entry">Plugin phrase entry, stored trimmed and lowercase</param>
    internal static bool IsAttachedSuffixScript(string entry) =>
        UsesNonSegmentedScript(entry) || entry.Any(IsHangulCharacter);

    private static bool EndsAtWordBoundary(string text, int end) =>
        end >= text.Length
            || !IsWordCharacter(text[end - 1])
            || !IsWordCharacter(text[end]);

    private static bool IsWordCharacter(char value)
    {
        if (char.IsLetter(value))
        {
            return true;
        }

        var category = char.GetUnicodeCategory(value);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }

    private static bool IsHangulCharacter(char value) =>
        value is >= HangulJamoStart and <= HangulJamoEnd
            or >= HangulCompatibilityJamoStart and <= HangulCompatibilityJamoEnd
            or >= HangulSyllablesStart and <= HangulSyllablesEnd;

    /// <summary>
    /// True when the text contains Thai, Kana or Han characters - a script written without word
    /// separators. Internal rather than private because the correction gate needs the same judgement for a
    /// different purpose: a character-count floor calibrated on Latin text is far too high for these
    /// scripts, where one character carries what a whole Latin word does, and a word-count floor is worse
    /// than useless because \p{L}+ tokenization makes an entire sentence a single token.
    /// </summary>
    internal static bool UsesNonSegmentedScript(string entry)
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
