// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message is an explicit abort of a
/// running recipe flow ("abbrechen", "vergiss es", "cancel", "never mind") rather than an answer
/// to the recipe's current slot question. It only fires on short messages that are essentially
/// just the cancellation itself, so a slot answer that merely contains a stop-like word is never
/// misread as an abort. Core languages (de/en/fr/it) are handled by hardcoded phrases. Plugin
/// language entries are loaded at startup via Configure() from conversation-signals.json files.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeCancellationDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private const int MaxTokensForCancellation = 6;

    private static readonly string[] CancellationPhrases =
    [
        // German
        "abbrechen", "abbruch", "brich ab", "hör auf", "hoer auf", "vergiss es", "lass es",
        "lass das", "doch nicht", "stopp",
        // English
        "cancel", "abort", "stop", "never mind", "nevermind", "forget it", "leave it",
        // French
        "annule", "annuler", "laisse tomber", "oublie ça", "oublie ca",
        // Italian
        "annulla", "annullare", "lascia stare", "lascia perdere", "non importa",
    ];

    private static readonly object _configureLock = new();
    private static string[] _pluginCancellationEntries = [];

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> cancellations)
    {
        lock (_configureLock)
        {
            _pluginCancellationEntries = PluginPhraseMatcher.Merge(_pluginCancellationEntries, cancellations);
        }
    }

    public static bool IsCancellation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count > MaxTokensForCancellation)
        {
            return false;
        }

        return PluginPhraseMatcher.MatchesAny(lower, tokens, CancellationPhrases)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginCancellationEntries);
    }
}
