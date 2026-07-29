// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts the trigger words of a recipe out of its trigger definition, in exactly the order and
/// with exactly the deduplication the knowledge index text has always used: per condition of allOf
/// first anyWordStart, then anySubstring, then startsWith; blanks dropped; finally duplicates removed
/// case-insensitively keeping the first occurrence. noneOf is deliberately not read - it describes
/// what must NOT appear in the utterance and would poison the embedding text with counter-examples.
/// Any change here re-hashes and re-embeds every recipe entry of the knowledge index.
/// </summary>
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeTriggerWordExtractor
{
    /// <summary>
    /// Returns the trigger words of the given trigger definition.
    /// </summary>
    /// <param name="trigger">Trigger definition of a recipe; null yields an empty list</param>
    public static List<string> Extract(RecipeTrigger? trigger)
    {
        if (trigger?.AllOf == null)
        {
            return [];
        }

        var words = new List<string>();

        foreach (var condition in trigger.AllOf)
        {
            if (condition == null)
            {
                continue;
            }

            Append(words, condition.AnyWordStart);
            Append(words, condition.AnySubstring);
            Append(words, condition.StartsWith);
        }

        return [.. words.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static void Append(List<string> target, List<string>? words)
    {
        if (words == null)
        {
            return;
        }

        foreach (var word in words)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                target.Add(word);
            }
        }
    }
}
