// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message opens with a refusal
/// ("nein", "no, not now", "non merci") — typically an answer declining an offer or question the
/// assistant made in its previous turn, and (IsBareNegation) whether the reply is nothing BUT a
/// refusal. The recipe engine uses it to keep such replies away from
/// the semantic recipe fallback: a decline carries no action intent, yet its embedding may land
/// in the grey zone of a mutation recipe and hijack the turn into a confirmation gate ("I found
/// two possible actions ..."). Only the first two word tokens are inspected, so a negation later
/// in a genuine request ("kannst du nicht ...") never suppresses matching. Core languages
/// (de/en/fr/it) are handled by hardcoded tokens. Plugin language entries are loaded at startup
/// via Configure() from conversation-signals.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class DeclineDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private const int LeadingTokensInspected = 2;

    private const int BareNegationMaxTokens = 3;

    private static readonly HashSet<string> LeadingNegationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "nein", "nee", "nö", "noe", "nicht", "nichts", "kein", "keine", "keinen",
        // English
        "no", "nope", "nah", "not", "nothing",
        // French
        "non", "rien",
        // Italian
        "niente", "nulla",
    };

    private static readonly object _configureLock = new();
    private static string[] _pluginNegationEntries = [];
    private static string[] _pluginDeclineEntries = [];

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> negations, IEnumerable<string> declines)
    {
        lock (_configureLock)
        {
            _pluginNegationEntries = PluginPhraseMatcher.Merge(_pluginNegationEntries, negations);
            _pluginDeclineEntries = PluginPhraseMatcher.Merge(_pluginDeclineEntries, declines);
        }
    }

    /// <summary>
    /// Discards every entry Configure ever merged in and restores the core-only state the detector starts
    /// in. Test-only: Configure writes process-wide static state additively, so without a way back a
    /// fixture that loads a language pack would decide the outcome of every fixture running after it.
    /// </summary>
    internal static void Reset()
    {
        lock (_configureLock)
        {
            _pluginNegationEntries = [];
            _pluginDeclineEntries = [];
        }
    }

    public static bool LeadsWithNegation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.TrimStart().ToLowerInvariant();

        var leadingTokens = WordPattern.Matches(message)
            .Take(LeadingTokensInspected)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (leadingTokens.Any(LeadingNegationTokens.Contains))
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
    /// Core-language tokens and single-token plugin negations both count, so a language-pack refusal
    /// ("nie", "لا", "아니요") answers the question just like the German "nein". What stays outside: a
    /// multi-word plugin phrase, which cannot be matched token-by-token, and an entry whose script carries
    /// combining marks, because the word pattern matches letters only (Thai "ไม่" tokenizes as "ไม"). A false
    /// negative does not degrade into the ordinary correction path: the pending branch of the trajectory
    /// capture returns either way, so the confirmation gate is simply never resolved.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    public static bool IsBareNegation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        return tokens.Count > 0
            && tokens.Count <= BareNegationMaxTokens
            && tokens.TrueForAll(IsNegationToken);
    }

    private static bool IsNegationToken(string token) =>
        LeadingNegationTokens.Contains(token)
        || Array.IndexOf(_pluginNegationEntries, token) >= 0;

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
