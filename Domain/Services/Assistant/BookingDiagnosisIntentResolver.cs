// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a turn must carry diagnose_work_booking and diagnose_planning_period (the same question without a shift,
/// answered from the membership, group and seal periods) together with explain_why_cannot_book: a "why ... not"
/// question (warum/wieso/weshalb ... nicht/kein, why ... not/n't/cannot, pourquoi ... pas/jamais, perché ... non) asked
/// on the schedule page or naming a day. Vector retrieval ranks the diagnosis below the general explanation for such
/// wording, and an Explain skill's graph edges never lead to a Read skill, so without this rule the concrete check of
/// the cell is often missing. Both skills only read; a false positive costs two tool slots. Recognition covers the core
/// languages de/en/fr/it only (like ScheduleReadIntentResolver); the other languages reach the skills through their
/// synonyms. Negation words match as whole words, so "nichts" or "nothing" do not count, while "kein" also matches
/// keine/keinen/keinem/keiner.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>
/// <param name="currentRoute">Frontend route from the page context.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class BookingDiagnosisIntentResolver
{
    private const string EnglishContractedNegation = "n't";
    private const string EnglishContractedNegationTypographic = "n’t";
    private const string GermanNegationStem = "kein";

    private static readonly string[] GuaranteedSkills =
        [SkillNames.DiagnoseWorkBooking, SkillNames.DiagnosePlanningPeriod, SkillNames.ExplainWhyCannotBook];

    private static readonly HashSet<string> WhyWords = new(StringComparer.Ordinal)
    {
        "warum", "wieso", "weshalb", "why", "pourquoi", "perché", "perche",
    };

    private static readonly HashSet<string> NegationWords = new(StringComparer.Ordinal)
    {
        "nicht", "nie", "not", "cannot", "never", "pas", "jamais", "non",
    };

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message, string? currentRoute)
    {
        if (!IsWhyNotQuestion(message))
        {
            return [];
        }

        return ScheduleReadIntentResolver.IsSchedulePage(currentRoute) || ScheduleReadIntentResolver.HasDateSignal(message)
            ? GuaranteedSkills
            : [];
    }

    private static bool IsWhyNotQuestion(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        var words = ScheduleReadIntentResolver.WordPattern.Matches(lower).Select(match => match.Value).ToList();
        if (!words.Any(WhyWords.Contains))
        {
            return false;
        }

        return words.Any(word => NegationWords.Contains(word) || IsGermanNegationArticle(word))
            || lower.Contains(EnglishContractedNegation, StringComparison.Ordinal)
            || lower.Contains(EnglishContractedNegationTypographic, StringComparison.Ordinal);
    }

    private static bool IsGermanNegationArticle(string word) =>
        word.StartsWith(GermanNegationStem, StringComparison.Ordinal) && word.Length <= GermanNegationStem.Length + 2;
}
