// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message reads as a correction of
/// the assistant's immediately preceding turn ("nein", "nicht das", "falsch", "no, wrong") rather
/// than a fresh, unrelated request. Word matching alone is deliberately too broad to use on its
/// own (common words like "nicht" appear in many ordinary German sentences) — the caller must
/// combine this signal with a short time window since the previous turn, so an implicit
/// correction is only inferred for a reactive, immediate follow-up, not any later message that
/// happens to contain a negation.
/// Core languages (de/en/fr/it) are handled by hardcoded tokens. Plugin language entries are
/// loaded at startup via Configure() from conversation-signals.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the follow-up turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ImplicitCorrectionDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> CorrectionTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "nein", "nicht", "falsch", "falsche", "falscher", "falsches",
        // English
        "no", "not", "wrong", "incorrect",
        // French
        "non", "faux", "incorrect",
        // Italian
        "no", "sbagliato", "errato",
    };

    private static readonly object _configureLock = new();
    private static string[] _pluginCorrectionEntries = [];

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> corrections)
    {
        lock (_configureLock)
        {
            _pluginCorrectionEntries = PluginPhraseMatcher.Merge(_pluginCorrectionEntries, corrections);
        }
    }

    public static bool IsCorrectionSignal(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        return tokens.Any(CorrectionTokens.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginCorrectionEntries);
    }
}
