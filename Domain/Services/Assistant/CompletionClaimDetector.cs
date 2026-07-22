// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether an assistant RESPONSE claims a state change was
/// already carried out ("habe den Kunden angelegt", "the client has been created", "erledigt",
/// "c'est fait"). Combined with a zero-tool turn, the orchestrator treats such a claim as a
/// no-action lie and appends an honest correction, independent of the user-side mutation-intent flag.
/// It favours precision over recall: a standalone completion marker, or a completion auxiliary
/// ("habe"/"wurde"/"have"/"has been"/"ai"/"ho") co-occurring with a past-participle completion verb
/// ("angelegt"/"created"/"créé"/"creato"). Present-tense, future ("wird … angelegt", "I will create")
/// and infinitive offers ("Ich kann … anlegen") deliberately do NOT match. Core languages (de/en/fr/it)
/// are handled by hardcoded tokens; plugin language entries are loaded at startup via Configure() from
/// completion-claim.json files in each language plugin.
/// </summary>
/// <param name="response">The assistant answer produced for the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class CompletionClaimDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> CompletionMarkerTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "erledigt", "done", "terminé", "termine", "completato",
    };

    private static readonly string[] CompletionMarkerPhrases =
    {
        "ist erledigt", "wurde erledigt", "so weit erledigt",
        "has been done", "is done", "all set",
        "c'est fait", "c est fait",
        "è fatto", "e fatto",
    };

    private static readonly HashSet<string> CompletionAuxiliaries = new(StringComparer.OrdinalIgnoreCase)
    {
        // German — perfect/past-passive auxiliaries only; the stative-present copula ("ist"/"sind") and
        // the passive-present "wird" are excluded so how-to answers ("ein Kunde ist/wird angelegt") do not match.
        "habe", "hab", "hat", "haben", "wurde", "wurden",
        // English — perfect auxiliaries only; the copula "is" is excluded so passive how-to answers
        // ("a client is created", "the number is saved") do not match.
        "have", "has", "been",
        // French — perfect auxiliaries only; the copula "est" is excluded.
        "ai", "été",
        // Italian — perfect auxiliaries only; the copula "è" is excluded ("è stato" is still caught via "stato").
        "ho", "hai", "ha", "stato", "stata",
    };

    private static readonly string[] AuxContractionPhrases =
    {
        // Perfect-auxiliary contractions only. The copula contractions "it's"/"that's" ("it is"/"that is")
        // are excluded for the same reason bare "is" is: they would match passive how-to answers.
        // "it's been created" is still caught via "been"; "that's done" via the "done" marker.
        "i've", "we've", "you've", "j'ai", "a été", "a ete",
    };

    private static readonly HashSet<string> CompletionParticiples = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "angelegt", "erstellt", "erfasst", "gelöscht", "geloescht", "entfernt", "aktualisiert",
        "hinzugefügt", "hinzugefuegt", "zugewiesen", "zugeordnet", "verschoben", "gespeichert",
        "geändert", "geaendert", "eingeplant", "umgeplant", "geschnitten", "umbenannt", "dupliziert",
        "gruppiert", "registriert", "eingetragen", "nachgetragen",
        // English
        "created", "added", "deleted", "removed", "updated", "assigned", "scheduled", "rescheduled",
        "saved", "moved", "renamed", "duplicated", "registered", "split", "changed",
        // French
        "créé", "cree", "créée", "creee", "ajouté", "ajoute", "supprimé", "supprime", "modifié", "modifie",
        "enregistré", "enregistre", "assigné", "assigne", "planifié", "planifie", "renommé", "renomme",
        // Italian
        "creato", "creata", "aggiunto", "aggiunta", "eliminato", "eliminata", "modificato", "salvato",
        "assegnato", "pianificato", "rinominato", "duplicato", "fatto",
    };

    private static readonly object _configureLock = new();
    private static string[] _pluginCompletionEntries = [];

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// CompletionClaimPluginLoader after reading completion-claim.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> completionClaims)
    {
        lock (_configureLock)
        {
            _pluginCompletionEntries = PluginPhraseMatcher.Merge(_pluginCompletionEntries, completionClaims);
        }
    }

    public static bool ClaimsCompletion(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var lower = response.ToLowerInvariant();

        var tokens = WordPattern.Matches(response)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        if (tokens.Any(CompletionMarkerTokens.Contains))
        {
            return true;
        }

        if (CompletionMarkerPhrases.Any(phrase => lower.Contains(phrase, StringComparison.Ordinal)))
        {
            return true;
        }

        var hasParticiple = tokens.Any(CompletionParticiples.Contains);
        if (hasParticiple)
        {
            var hasAuxiliary = tokens.Any(CompletionAuxiliaries.Contains)
                || AuxContractionPhrases.Any(phrase => lower.Contains(phrase, StringComparison.Ordinal));
            if (hasAuxiliary)
            {
                return true;
            }
        }

        return PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginCompletionEntries);
    }
}
