// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message expresses a navigation
/// intent — go to, scroll to, open, or switch to a specific page or section — so the orchestrator
/// can force tool_choice="required" for the navigate_to skill.
/// Core languages (de/en/fr/it) are handled by hardcoded stems. Plugin language keywords are
/// loaded at startup via Configure() from navigation-intent.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class NavigationIntentDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> InfoQuestionLeads = new(StringComparer.OrdinalIgnoreCase)
    {
        "wie", "warum", "weshalb", "wieso", "was", "wann", "wo", "woher", "wohin", "wer", "wem", "wen",
        "welche", "welcher", "welches", "welchen",
        "how", "what", "why", "when", "where", "who", "whom", "which",
        "comment", "pourquoi", "quand", "ou", "où", "qui", "quel", "quelle", "quels", "quelles",
        "come", "perche", "perché", "quando", "dove", "chi", "quale", "quali",
    };

    private static readonly HashSet<string> DirectionalMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "zu", "zur", "zum", "to",
    };

    private static readonly object _configureLock = new();
    private static HashSet<string> _pluginQuestionLeads = new(StringComparer.OrdinalIgnoreCase);
    private static string[] _pluginNavigationPhrases = [];

    /// <summary>
    /// Extends detection with plugin language keywords. Called once at startup by
    /// NavigationIntentPluginLoader after reading navigation-intent.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> questionLeads, IEnumerable<string> navigationPhrases)
    {
        lock (_configureLock)
        {
            var leads = new HashSet<string>(_pluginQuestionLeads, StringComparer.OrdinalIgnoreCase);
            foreach (var lead in questionLeads)
                leads.Add(lead);
            _pluginQuestionLeads = leads;

            var phrases = _pluginNavigationPhrases
                .Concat(navigationPhrases.Select(p => p.Trim().ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _pluginNavigationPhrases = phrases;
        }
    }

    public static bool IsNavigationIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var lower = message.ToLowerInvariant();

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0)
            return false;

        var firstToken = tokens[0];
        if (InfoQuestionLeads.Contains(firstToken) || _pluginQuestionLeads.Contains(firstToken))
            return false;

        // For non-tokenizable languages (CJK, Thai, etc.) suppress question phrases as prefix
        var pluginLeads = _pluginQuestionLeads;
        if (pluginLeads.Count > 0 && pluginLeads.Any(lead => lower.StartsWith(lead, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Core language stem-based detection (de/en/fr/it)
        if (tokens.Any(t => t.StartsWith("scroll", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (tokens.Any(t => t.StartsWith("navig", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (tokens.Any(t =>
            t.StartsWith("öffn", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("open", StringComparison.OrdinalIgnoreCase)))
            return true;

        var hasDirectional = tokens.Any(t => DirectionalMarkers.Contains(t));
        if (hasDirectional && tokens.Any(t =>
            t.StartsWith("geh", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("go", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("wechsl", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("spring", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("bring", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Plugin language phrase-based detection (substring match, language-agnostic merge)
        var phrases = _pluginNavigationPhrases;
        if (phrases.Length > 0 && phrases.Any(p => lower.Contains(p)))
            return true;

        return false;
    }
}
