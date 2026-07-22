// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic, language-neutral heuristic that decides whether a chat message is a plan
/// candidate: it holds several state-changing (mutation) targets in one message AND no guided
/// recipe already covers it. Multiple targets are counted by splitting the message on sentence /
/// clause punctuation and counting the segments that MutationIntentDetector flags as a mutation
/// intent — no new language-sensitive phrase list is introduced, so every core and plugin language
/// is served by the existing detector. Precision-biased: the AND with "no recipe match" keeps the
/// nudge off single-action and recipe-covered turns.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>
/// <param name="recipeMatched">True when a deterministic or engine recipe already forces a flow for this message.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class PlanTriggerHeuristic
{
    private static readonly Regex SegmentSeparators = new(@"[,;.:!?\r\n\t]+", RegexOptions.Compiled);

    public static bool IsPlanCandidate(string? message, bool recipeMatched)
    {
        if (recipeMatched || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return CountMutationSegments(message) >= PlanSkillDefaults.MinMutationTargets;
    }

    public static int CountMutationSegments(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return 0;
        }

        var count = 0;
        foreach (var segment in SegmentSeparators.Split(message))
        {
            if (MutationIntentDetector.IsMutationIntent(segment))
            {
                count++;
            }
        }

        return count;
    }
}
