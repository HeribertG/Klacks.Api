// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether an assistant answer was an outright refusal ("I cannot do that"), which is the only
/// evidence the learning loop has that a capability was missing. Inherited unchanged from the retired
/// SkillGapDetector, including its blind spot: a model that invents an answer instead of admitting the
/// gap produces no signal at all. Sits next to the other conversation-signal detectors so the language
/// plugin loader configures them all the same way.
/// </summary>
/// <param name="response">Raw assistant answer of the finished turn</param>
/// <param name="phrases">Refusal phrases contributed by a language plugin's conversation-signals.json</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RefusalSignalDetector
{
    private static readonly string[] CorePhrases =
    [
        "kann ich leider nicht",
        "diese funktion gibt es nicht",
        "habe ich keinen zugriff",
        "ist nicht möglich",
        "bin leider nicht in der lage",
        "diese fähigkeit habe ich nicht",
        "dafür habe ich keine",
        "i cannot do that",
        "i don't have the ability",
        "i'm unable to",
        "i do not have access",
        "that functionality is not available",
        "i lack the capability",
        "no skill available",
        "this is not supported",
        "i cannot perform",
        "unfortunately i cannot",
        "i am not able to",
        "je ne peux pas",
        "je n'ai pas accès",
        "ce n'est pas possible",
        "cette fonction n'existe pas",
        "non posso",
        "non ho accesso",
        "non è possibile",
        "questa funzione non esiste"
    ];

    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);
    private static readonly object ConfigureLock = new();
    private static string[] _pluginPhrases = [];

    /// <summary>
    /// Extends refusal detection with plugin language phrases. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> phrases)
    {
        lock (ConfigureLock)
        {
            _pluginPhrases = PluginPhraseMatcher.Merge(_pluginPhrases, phrases);
        }
    }

    public static bool IsRefusal(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var lower = response.ToLowerInvariant();
        if (CorePhrases.Any(phrase => lower.Contains(phrase, StringComparison.Ordinal)))
        {
            return true;
        }

        var tokens = WordPattern.Matches(response)
            .Select(match => match.Value.ToLowerInvariant())
            .ToList();

        return PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginPhrases);
    }
}
