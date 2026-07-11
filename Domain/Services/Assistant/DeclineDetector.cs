// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message opens with a refusal
/// ("nein", "no, not now", "non merci") — typically an answer declining an offer or question the
/// assistant made in its previous turn. The recipe engine uses it to keep such replies away from
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
