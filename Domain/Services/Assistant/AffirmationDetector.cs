// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message is a short affirmation /
/// go-ahead ("ja", "ok", "mach", "weiter", "yes", "do it") that confirms a previously requested
/// action. Combined with an outstanding pending confirmation, the orchestrator uses it to force a
/// real tool call so the model actually executes confirm_pending_action instead of replying in prose.
/// It deliberately favours precision over recall: a negation token anywhere ("nein", "nicht",
/// "abbrechen", "no", "cancel") or a trailing question suppresses the signal, so an ambiguous reply
/// like "ja, aber nicht heute" or "ja? was kostet das" leaves the existing "auto" behaviour intact.
/// Core languages (de/en/fr/it) are handled by hardcoded tokens. Plugin language entries are
/// loaded at startup via Configure() from conversation-signals.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AffirmationDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> AffirmationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "ja", "jawohl", "jep", "jo", "klar", "ok", "okay", "okey", "gerne", "bitte", "genau",
        "passt", "stimmt", "richtig", "mach", "machen", "los", "weiter", "fortfahren", "ausführen",
        "ausfuehren", "bestätige", "bestaetige", "bestätigen", "bestaetigen", "bestätigt",
        "einverstanden", "sicher", "natürlich", "natuerlich",
        // English
        "yes", "yep", "yeah", "yup", "sure", "confirm", "confirmed", "proceed", "continue",
        "go", "do", "please", "correct", "absolutely", "affirmative",
        // French
        "oui", "ouais", "daccord", "confirme", "confirmer", "continuer",
        // Italian
        "si", "sì", "certo", "conferma", "confermare", "procedi", "vai",
    };

    private static readonly HashSet<string> NegationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "nein", "nicht", "kein", "keine", "keinen", "stop", "stopp", "abbrechen", "abbruch",
        "halt", "warte",
        // English
        "no", "not", "dont", "cancel", "wait", "stop",
        // French / Italian
        "non", "pas", "annuler",
    };

    private static readonly object _configureLock = new();
    private static string[] _pluginAffirmationEntries = [];
    private static string[] _pluginNegationEntries = [];

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> affirmations, IEnumerable<string> negations)
    {
        lock (_configureLock)
        {
            _pluginAffirmationEntries = PluginPhraseMatcher.Merge(_pluginAffirmationEntries, affirmations);
            _pluginNegationEntries = PluginPhraseMatcher.Merge(_pluginNegationEntries, negations);
        }
    }

    public static bool IsAffirmation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Contains('?'))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0
            || tokens.Any(NegationTokens.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginNegationEntries))
        {
            return false;
        }

        return tokens.Any(AffirmationTokens.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginAffirmationEntries);
    }
}
