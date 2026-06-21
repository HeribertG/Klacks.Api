// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message expresses a state-changing
/// (mutation) intent — create / update / delete / cut / assign / schedule — so the orchestrator can
/// force tool_choice="required". It deliberately favours precision over recall: a false negative only
/// leaves the existing "auto" behaviour, whereas a false positive would force a spurious tool call on
/// the well-behaved default path. A leading information interrogative ("Wie erstelle ich …?") therefore
/// suppresses the signal even when a mutation verb appears later in the sentence.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class MutationIntentDetector
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

    private static readonly string[] MutationStems =
    {
        // German
        "erstell", "erfass", "anleg", "anzuleg", "hinzufüg", "lösch", "loesch", "entfern", "aktualisier",
        "schneid", "zuweis", "zuordn", "einplan", "umplan", "registrier", "speicher", "bearbeit",
        "verschieb", "umbenenn", "dupliz",
        // English
        "creat", "delet", "remov", "updat", "assign", "schedul", "reschedul", "register", "rename", "duplicat",
        // French
        "créer", "crée", "cré", "ajout", "supprim", "modifi", "planifi", "enregistr", "renomm",
        // Italian
        "crea", "aggiung", "elimin", "modific", "assegn", "pianific", "registra", "rinomin",
    };

    private static readonly HashSet<string> MutationExactTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "edit", "cut", "split", "change", "changed",
        "buche", "buchen", "plane", "planen", "ändere", "aendere", "schneide",
        "ajoute", "modifie", "supprime", "aggiungi",
    };

    public static bool IsMutationIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0 || InfoQuestionLeads.Contains(tokens[0]))
        {
            return false;
        }

        foreach (var token in tokens)
        {
            if (MutationExactTokens.Contains(token))
            {
                return true;
            }

            foreach (var stem in MutationStems)
            {
                if (token.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // German separable verbs split across the sentence: "lege einen Kunden an", "weise … zu",
        // "plane … ein", "füge … hinzu".
        if (tokens.Contains("an") && tokens.Any(t => t.StartsWith("leg", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Contains("zu") && tokens.Any(t => t.StartsWith("weis", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Contains("ein") && tokens.Any(t => t.StartsWith("plan", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return tokens.Any(t => t.StartsWith("hinzu", StringComparison.OrdinalIgnoreCase));
    }
}
