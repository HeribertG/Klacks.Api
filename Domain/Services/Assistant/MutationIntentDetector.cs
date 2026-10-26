// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Precision-biased detector that decides whether a user chat message expresses a state-changing
/// (mutation) intent — create / update / delete / cut / assign / schedule — so the orchestrator can
/// force tool_choice="required". It deliberately favours precision over recall: a false negative only
/// leaves the existing "auto" behaviour, whereas a false positive would force a spurious tool call on
/// the well-behaved default path. A leading information interrogative ("Wie erstelle ich …?") therefore
/// suppresses the signal even when a mutation verb appears later in the sentence.
/// Core languages (de/en/fr/it) are handled by hardcoded stems. Plugin language keywords are
/// loaded at startup via Configure() from mutation-intent.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class MutationIntentDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private const char LatinExtendedBEnd = 'ɏ';
    private const char LatinExtendedAdditionalStart = 'Ḁ';
    private const char LatinExtendedAdditionalEnd = 'ỿ';

    private static readonly HashSet<string> InfoQuestionLeads = new(StringComparer.OrdinalIgnoreCase)
    {
        "wie", "warum", "weshalb", "wieso", "was", "wann", "wo", "woher", "wohin", "wer", "wem", "wen",
        "welche", "welcher", "welches", "welchen",
        "how", "what", "why", "when", "where", "who", "whom", "which",
        "comment", "pourquoi", "quand", "ou", "où", "qui", "quel", "quelle", "quels", "quelles",
        "come", "perche", "perché", "quando", "dove", "chi", "quale", "quali", "cosa", "cos", "qual",
    };

    private static readonly object _configureLock = new();
    private static HashSet<string> _pluginQuestionLeads = new(StringComparer.OrdinalIgnoreCase);
    private static string[] _pluginMutationPhrases = [];

    /// <summary>
    /// Extends detection with plugin language keywords. Called once at startup by
    /// MutationIntentPluginLoader after reading mutation-intent.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> questionLeads, IEnumerable<string> mutationPhrases)
    {
        lock (_configureLock)
        {
            var leads = new HashSet<string>(_pluginQuestionLeads, StringComparer.OrdinalIgnoreCase);
            foreach (var lead in questionLeads)
                leads.Add(lead);
            _pluginQuestionLeads = leads;

            var phrases = _pluginMutationPhrases
                .Concat(mutationPhrases.Select(p => p.Trim().ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _pluginMutationPhrases = phrases;
        }
    }

    private static readonly string[] MutationStems =
    {
        // German
        "erstell", "erfass", "anleg", "anzuleg", "hinzufüg", "lösch", "loesch", "entfern", "aktualisier",
        "schneid", "zuweis", "zuordn", "einplan", "umplan", "registrier", "speicher", "bearbeit",
        "verschieb", "umbenenn", "dupliz", "gruppier",
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
        // W4: German imperative forms of common mutation verbs ("Mach eine neue Spamregel",
        // "Setz den Haken", "Leg einen Kunden an"). Only unambiguous forms — "macht" is skipped
        // because it collides with the noun "Macht", "stelle" with the noun "Stelle".
        "leg", "lege", "mach", "mache", "setz", "setze", "füg", "füge",
        // German imperatives and infinitives of further change verbs. Exact tokens only: the stems would also match nouns
        // and participles ("Sperrzeit", "Streichung", "Einrichtung", "installierte Module", "Schaltfläche", "Merkmal").
        "korrigier", "korrigiere", "korrigieren", "berichtig", "berichtige", "berichtigen",
        "sperr", "sperre", "sperren", "verwerf", "verwerfe", "verwerfen", "verwirf",
        "streich", "streiche", "streichen", "stopp", "stoppe", "stoppen",
        "deaktivier", "deaktiviere", "deaktivieren", "aktivier", "aktiviere", "aktivieren",
        "fixier", "fixiere", "fixieren", "importier", "importiere", "importieren",
        "installier", "installiere", "installieren", "uninstallier", "uninstalliere", "deinstalliere",
        "übernimm", "übernehme", "übernehmen", "schalt", "schalte", "bündel", "bündle", "merk", "merke",
        "lös", "löse", "starte", "füll", "fülle", "erhöh", "erhöhe", "erhöhen", "wechsl", "wechsle", "wechseln",
    };

    // German separable verbs: the stem and its particle stand apart in the sentence ("häng … an", "stell … um").
    private static readonly (string Stem, string Particle)[] SeparableVerbs =
    {
        ("häng", "an"), ("stell", "um"), ("benenn", "um"), ("teil", "auf"), ("heb", "an"), ("richt", "ein"),
        ("hol", "zurück"), ("pass", "an"), ("schliess", "ab"), ("schließ", "ab"),
    };

    /// <summary>
    /// True when the message is a plain information question — it starts with a question lead
    /// ("welche"/"what"/"how"/…, core set plus per-plugin leads, incl. CJK prefixes). Lets callers keep
    /// read questions from being force-matched onto the mutation-only recipe catalogue.
    /// </summary>
    public static bool IsInformationQuestion(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count > 0
            && (InfoQuestionLeads.Contains(tokens[0]) || _pluginQuestionLeads.Contains(tokens[0])))
        {
            return true;
        }

        var pluginLeads = _pluginQuestionLeads;
        return pluginLeads.Count > 0
            && pluginLeads.Any(lead => lead.Length > 0
                                       && lower.StartsWith(lead, StringComparison.OrdinalIgnoreCase)
                                       && (!IsLatinLetter(lead[^1]) || IsWordBoundary(lower, lead.Length)));
    }

    private static bool IsWordBoundary(string text, int index) =>
        index >= text.Length || !char.IsLetterOrDigit(text[index]);

    private static bool IsLatinLetter(char character) =>
        char.IsLetter(character)
        && (character <= LatinExtendedBEnd
            || (character >= LatinExtendedAdditionalStart && character <= LatinExtendedAdditionalEnd));

    public static bool IsMutationIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0 || IsInformationQuestion(message))
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

        // "ordne … zu" (zuordnen) — separable form of the joined "zuordn" stem above.
        if (tokens.Contains("zu") && tokens.Any(t => t.StartsWith("ordn", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Contains("ein") && tokens.Any(t => t.StartsWith("plan", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Any(t => t.StartsWith("hinzu", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // W4: separable imperatives "stell … ein" (einstellen) and "trag … ein" (eintragen).
        if (tokens.Contains("ein") && tokens.Any(t => t.StartsWith("stell", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Contains("ein") && tokens.Any(t => t.StartsWith("trag", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (SeparableVerbs.Any(verb =>
                tokens.Contains(verb.Particle) && tokens.Any(t => t.StartsWith(verb.Stem, StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        var phrases = _pluginMutationPhrases;
        return phrases.Length > 0 && phrases.Any(p => lower.Contains(p));
    }
}
