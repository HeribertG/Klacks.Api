// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a turn must carry explain_page_schedule and diagnose_planning_period: a question about the shading of the
/// schedule cells ("Warum sind die Zellen dunkelblau", "what does the colour of the cells mean") asked on the schedule page.
/// A day outside the employment is drawn dark and a day outside the group membership blue, and users name both by their
/// colour, so the wording carries none of the keywords vector retrieval ranks on; without the diagnosis the model guesses
/// causes (grid colour settings, weekends, sealed days). Two signals count: a shading word (dark, blue) anywhere in the
/// message, or a colour word together with a cell word. Both skills only read; a false positive costs two tool slots, so the
/// rule is liberal. Recognition covers the core languages de/en/fr/it only (like ScheduleReadIntentResolver); the other
/// languages reach the skills through their synonyms. It is limited to the schedule page, where the shading is meant.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>
/// <param name="currentRoute">Frontend route from the page context.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GridMarkingIntentResolver
{
    private static readonly string[] GuaranteedSkills = [SkillNames.ExplainPageSchedule, SkillNames.DiagnosePlanningPeriod];

    private static readonly string[] ShadingStems =
    [
        "dunkel", "blau", "dark", "blue", "bleu", "foncé", "fonce", "sombre", "scur",
    ];

    private static readonly HashSet<string> ShadingWords = new(StringComparer.Ordinal) { "blu" };

    private static readonly string[] ColourStems = ["farb", "färb", "color", "colour", "couleur"];

    private static readonly string[] CellStems =
    [
        "zell", "feld", "kachel", "cell", "tile", "cellule", "cella", "celle", "champ", "campo",
    ];

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message, string? currentRoute)
    {
        if (string.IsNullOrWhiteSpace(message) || !ScheduleReadIntentResolver.IsSchedulePage(currentRoute))
        {
            return [];
        }

        var words = ScheduleReadIntentResolver.WordPattern.Matches(message.ToLowerInvariant())
            .Select(match => match.Value)
            .ToList();

        return IsShadingQuestion(words) ? GuaranteedSkills : [];
    }

    private static bool IsShadingQuestion(IReadOnlyList<string> words) =>
        words.Any(IsShadingWord) || (words.Any(word => StartsWithAny(word, ColourStems)) && words.Any(word => StartsWithAny(word, CellStems)));

    private static bool IsShadingWord(string word) => ShadingWords.Contains(word) || StartsWithAny(word, ShadingStems);

    private static bool StartsWithAny(string word, string[] stems) =>
        stems.Any(stem => word.StartsWith(stem, StringComparison.Ordinal));
}
