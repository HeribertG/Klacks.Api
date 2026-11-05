// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a message asks to see the schedule with the availabilities or the planning commands (keywords) switched
/// on ("Zeig mir den Dezember mit Verfügbarkeiten und Keywords"). Only open_schedule can set those switches; vector
/// retrieval does not rank it for such a wording, the toolset then offers only the availability and schedule explain
/// skills, and the model falls back to navigate_to, which opens the page without them (live 2026-11-05). Three signals must
/// hold together: a display verb, an overlay noun (availabilities or commands/keywords) and a schedule anchor, which is a
/// schedule word or a date signal (a month, weekday, relative day or numeric date, ScheduleReadIntentResolver.HasDateSignal),
/// so "Zeig mir die Verfügbarkeiten von Mia" on the availability page asks for no schedule. Core languages de/en/fr/it,
/// matched on word starts; a plugin-language wording does not count. Visibility only: the model still decides the call.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ScheduleOverlayIntentResolver
{
    public const string OpenScheduleSkillName = "open_schedule";

    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly string[] DisplayVerbStems =
    [
        "zeig", "anzeig", "öffne", "oeffne", "blend", "schalt",
        "show", "open", "display", "view",
        "montre", "affiche", "ouvre", "ouvrir",
        "mostra", "apri", "visualizz",
    ];

    private static readonly string[] OverlayNounStems =
    [
        "verfügbar", "verfuegbar", "keyword", "stichwort", "befehl", "planungsbefehl",
        "availab", "command",
        "disponib", "commande", "mots",
        "comand", "parole",
    ];

    private static readonly string[] ScheduleWordStems =
    [
        "dienstplan", "einsatzplan", "schedule", "planning", "plan", "calendrier", "calendario", "programma",
    ];

    /// <summary>True when the message asks to see the schedule with the availabilities or the planning commands shown.</summary>
    /// <param name="message">The current user chat message</param>
    public static bool IsOverlayRequest(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || MutationIntentDetector.IsInformationQuestion(message))
        {
            return false;
        }

        var words = WordPattern.Matches(message.ToLowerInvariant()).Select(match => match.Value).ToList();
        return words.Any(word => StartsWithAny(word, DisplayVerbStems))
            && words.Any(word => StartsWithAny(word, OverlayNounStems))
            && (words.Any(word => StartsWithAny(word, ScheduleWordStems)) || ScheduleReadIntentResolver.HasDateSignal(message));
    }

    private static bool StartsWithAny(string word, string[] stems) =>
        stems.Any(stem => word.StartsWith(stem, StringComparison.Ordinal));
}
