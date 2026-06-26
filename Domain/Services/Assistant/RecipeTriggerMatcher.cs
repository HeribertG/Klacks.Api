// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Matches a chat message against a recipe trigger. A trigger fires when every condition in allOf
/// matches AND no condition in noneOf matches. A single condition matches when any of its present
/// lists hits: anyWordStart (a stem at a word boundary, so mid-word false friends like "Pflege"⊃"lege"
/// do not trigger), anySubstring (case-insensitive contains), or startsWith (trimmed prefix).
/// Plugin-language synonyms (passed in for the detected language) act as a whole-recipe OR shortcut:
/// when the structured allOf does not match, any synonym appearing as a substring fires the recipe,
/// still subject to the same noneOf guard.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeTriggerMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public static bool Matches(RecipeTrigger trigger, string? message)
        => Matches(trigger, null, message);

    public static bool Matches(RecipeTrigger trigger, IReadOnlyCollection<string>? synonyms, string? message)
    {
        if (trigger == null || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (trigger.NoneOf.Any(c => ConditionMatches(c, message)))
        {
            return false;
        }

        if (trigger.AllOf.Count > 0 && trigger.AllOf.All(c => ConditionMatches(c, message)))
        {
            return true;
        }

        return synonyms is { Count: > 0 }
            && synonyms.Any(s => !string.IsNullOrWhiteSpace(s)
                && message.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ConditionMatches(RecipeCondition condition, string message)
    {
        if (condition.AnyWordStart is { Count: > 0 } && MatchesWordStart(condition.AnyWordStart, message))
        {
            return true;
        }

        if (condition.AnySubstring is { Count: > 0 }
            && condition.AnySubstring.Any(s => message.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (condition.StartsWith is { Count: > 0 })
        {
            var trimmed = message.TrimStart();
            if (condition.StartsWith.Any(s => trimmed.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWordStart(IReadOnlyList<string> stems, string message)
    {
        var pattern = @"\b(?:" + string.Join('|', stems.Select(Regex.Escape)) + ")";
        return Regex.IsMatch(message, pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    }
}
