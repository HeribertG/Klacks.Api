// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message opens with a refusal
/// ("nein", "no, not now", "non merci") — typically an answer declining an offer or question the
/// assistant made in its previous turn, and (IsBareNegation) whether the reply is nothing BUT a
/// refusal. The recipe engine uses it to keep such replies away from
/// the semantic recipe fallback: a decline carries no action intent, yet its embedding may land
/// in the grey zone of a mutation recipe and hijack the turn into a confirmation gate ("I found
/// two possible actions ..."). Only the first two word tokens are inspected, so a negation later
/// in a genuine request ("kannst du nicht ...") never suppresses matching. No language is named in
/// this code: the core vocabulary arrives through ConfigureCore from conversation-signals-core.json,
/// the plugin vocabulary through Configure from each language plugin's conversation-signals.json.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text;
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class DeclineDetector
{
    // One word is a letter followed by its combining marks, repeated. A bare \p{L}+ dropped the mark and
    // split the word: the Thai refusal "ไม่" tokenized as "ไม", which matches no vocabulary entry at all,
    // and the same held for every script that writes a word with combining marks (Devanagari, Arabic).
    private static readonly Regex WordPattern = new(@"(?:\p{L}\p{M}*)+", RegexOptions.Compiled);

    private const int LeadingTokensInspected = 2;

    private const int BareNegationMaxTokens = 3;

    private static readonly object _configureLock = new();
    private static HashSet<string> _coreNegationTokens = new(StringComparer.OrdinalIgnoreCase);
    private static string[] _pluginNegationEntries = [];
    private static string[] _pluginDeclineEntries = [];

    /// <summary>
    /// Installs the core-language negation vocabulary. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals-core.json. Kept apart from the
    /// plugin entries because a core entry is matched as a whole token only, never as a prefix: a short
    /// core token matched by prefix would fire on every word that starts with it. Reset keeps it, so the
    /// core vocabulary is installed once per process.
    /// </summary>
    public static void ConfigureCore(IEnumerable<string> negationTokens)
    {
        lock (_configureLock)
        {
            var merged = new HashSet<string>(_coreNegationTokens, StringComparer.OrdinalIgnoreCase);
            foreach (var token in negationTokens)
            {
                var normalized = Normalize(token).Trim();
                if (normalized.Length > 0)
                {
                    merged.Add(normalized);
                }
            }

            _coreNegationTokens = merged;
        }
    }

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> negations, IEnumerable<string> declines)
    {
        lock (_configureLock)
        {
            _pluginNegationEntries = PluginPhraseMatcher.Merge(_pluginNegationEntries, negations.Select(Normalize));
            _pluginDeclineEntries = PluginPhraseMatcher.Merge(_pluginDeclineEntries, declines.Select(Normalize));
        }
    }

    /// <summary>
    /// Discards every entry Configure ever merged in and restores the state the detector has once
    /// ConfigureCore has run, i.e. the core vocabulary alone. Test-only: Configure writes process-wide
    /// static state additively, so without a way back a fixture that loads a language pack would decide
    /// the outcome of every fixture running after it.
    /// </summary>
    internal static void Reset()
    {
        lock (_configureLock)
        {
            _pluginNegationEntries = [];
            _pluginDeclineEntries = [];
        }
    }

    /// <summary>
    /// Discards the core vocabulary ConfigureCore installed. Test-only, and deliberately separate from
    /// Reset, which keeps the core: a fixture measuring one language pack on its own has to clear the core
    /// as well, or a core token of an unrelated language stands in for a pack entry that does not actually
    /// match by itself - "nie" is a German core negation and a Polish pack entry, so the Polish pack would
    /// pass on the German vocabulary. A caller that clears the core owns putting it back, through
    /// ConversationSignalsPluginLoader.LoadCore, because every fixture afterwards shares this state.
    /// </summary>
    internal static void ResetCore()
    {
        lock (_configureLock)
        {
            _coreNegationTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static bool LeadsWithNegation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = Normalize(message);
        var lower = normalized.TrimStart().ToLowerInvariant();

        var leadingTokens = WordPattern.Matches(normalized)
            .Take(LeadingTokensInspected)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        var coreTokens = _coreNegationTokens;
        if (leadingTokens.Any(coreTokens.Contains))
        {
            return true;
        }

        return MatchesLeading(lower, leadingTokens, _pluginNegationEntries)
            || MatchesLeading(lower, leadingTokens, _pluginDeclineEntries);
    }

    /// <summary>
    /// True when the message consists of nothing but negation words and punctuation — at most
    /// <c>BareNegationMaxTokens</c> of them. This is the shape of an answer to a yes/no question
    /// ("Nein.", "No"), as opposed to a negation that corrects course ("Nein, nimm stattdessen ...").
    /// Core tokens, single-token plugin negations and multi-word plugin phrases all count, so a
    /// language-pack refusal ("nie", "لا", "아니요", "ahora no", "ไม่ ขอบคุณ") answers the question just like
    /// "nein" does. A phrase has no token boundary to test, so it is matched as a prefix - but only when
    /// what follows it carries no word at all, otherwise a refusal that then states a different wish
    /// would read as a bare one. A false negative does not degrade into the ordinary correction path: the
    /// pending branch of the trajectory capture returns either way, so the confirmation gate is simply
    /// never resolved.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    public static bool IsBareNegation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = Normalize(message);

        var tokens = WordPattern.Matches(normalized)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count > 0
            && tokens.Count <= BareNegationMaxTokens
            && tokens.TrueForAll(IsNegationToken))
        {
            return true;
        }

        var lower = normalized.Trim().ToLowerInvariant();
        return IsNothingButPhrase(lower, _pluginNegationEntries)
            || IsNothingButPhrase(lower, _pluginDeclineEntries);
    }

    private static bool IsNothingButPhrase(string lowerMessage, string[] entries)
    {
        foreach (var entry in entries)
        {
            if (lowerMessage.StartsWith(entry, StringComparison.Ordinal)
                && !WordPattern.IsMatch(lowerMessage[entry.Length..]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNegationToken(string token) =>
        _coreNegationTokens.Contains(token)
        || Array.IndexOf(_pluginNegationEntries, token) >= 0;

    /// <summary>
    /// Composed (NFC) form of the text, so a message whose refusal arrives decomposed still matches the
    /// composed vocabulary entry. Invalid surrogate input is passed through rather than thrown at: this
    /// runs on every chat turn, and a malformed message must cost a detection, not the turn.
    /// </summary>
    private static string Normalize(string? value)
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

    /// <summary>
    /// Separators a negation lead may be followed by before the rest of the clause starts: ASCII comma,
    /// colon and dash (plain and en/em), plus the CJK equivalents - ideographic comma '、', fullwidth
    /// comma '，' and fullwidth colon '：' - since zh/ja negation leads (plugin-configured) are followed
    /// by these instead.
    /// </summary>
    private static readonly char[] LeadSeparators =
        [' ', '\t', ',', ':', '-', '–', '—', '、', '，', '：'];

    /// <summary>
    /// Removes a message's leading negation word (core-language or a single-token plugin entry) together
    /// with a following separator (see LeadSeparators) and whitespace. A caller correcting the previous
    /// turn opens with "Nein, ..." - the negation and what follows share one clause with no sentence
    /// terminator between them, so a sentence-splitting detector (RecipeTopicSwitchDetector) can never
    /// see the part after the comma as its own sentence. Stripping the lead here lets such a caller
    /// re-check the remainder on its own.
    /// Returns the message unchanged when it does not lead with a negation, or when the lead is a
    /// multi-word plugin phrase - a phrase has no single token boundary to strip at.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    internal static string StripNegationLead(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var match = WordPattern.Match(message);
        if (!match.Success)
        {
            return message;
        }

        var leadToken = match.Value.ToLowerInvariant();
        if (!IsNegationToken(leadToken))
        {
            return message;
        }

        var remainder = message[(match.Index + match.Length)..];
        return remainder.TrimStart(LeadSeparators);
    }

    // Plugin entries must match at the START of the message (single token among the leading
    // tokens, or as a prefix for multi-word phrases and non-segmented scripts) — a mid-sentence
    // hit would reintroduce the false positives the leading-token rule exists to avoid.
    private static bool MatchesLeading(string lowerMessage, IReadOnlyCollection<string> leadingTokens, string[] entries)
    {
        foreach (var entry in entries)
        {
            if (leadingTokens.Contains(entry) || lowerMessage.StartsWith(entry, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
