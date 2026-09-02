// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Matches a chat message against a recipe trigger. A trigger fires when every condition in allOf
/// matches AND no condition in noneOf matches. A single condition matches when any of its present
/// lists hits: anyWordStart (a stem at a word boundary, so mid-word false friends like "Pflege"⊃"lege"
/// do not trigger), anySubstring (case-insensitive contains), or startsWith (an opening of the
/// message; a term written WITH a trailing space is matched as a whole word, a term without one as an
/// open stem - see MatchesStartsWith).
/// Plugin-language synonyms (passed in for the detected language) act as a whole-recipe OR shortcut:
/// when the structured allOf does not match, any synonym appearing as a substring fires the recipe,
/// still subject to the same noneOf guard. IsVetoed exposes the noneOf check on its own so the
/// semantic fallback can honor a recipe's exclusion vocabulary as well.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeTriggerMatcher
{
    // Wall-clock, not CPU time: the pattern itself is a plain alternation with no backtracking trap,
    // so a match that exceeds this budget means the machine was busy, not that the input was hostile.
    // The former 100 ms tripped under nothing worse than a concurrent build and made the trigger
    // disjointness gate flaky. One second still stops a pathological pattern long before a user notices.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private const string TimeoutMessage =
        "Recipe trigger regex timed out after {Timeout} on a {Length}-character message; treating the " +
        "condition as no match. The turn continues without a recipe.";

    public static bool Matches(RecipeTrigger trigger, string? message)
        => Matches(trigger, null, message, null);

    public static bool Matches(RecipeTrigger trigger, IReadOnlyCollection<string>? synonyms, string? message)
        => Matches(trigger, synonyms, message, null);

    public static bool Matches(
        RecipeTrigger trigger, IReadOnlyCollection<string>? synonyms, string? message, ILogger? logger)
    {
        if (trigger == null || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (IsVetoed(trigger, message, logger))
        {
            return false;
        }

        if (trigger.AllOf.Count > 0 && trigger.AllOf.All(c => ConditionMatches(c, message, logger)))
        {
            return true;
        }

        return synonyms is { Count: > 0 }
            && synonyms.Any(s => !string.IsNullOrWhiteSpace(s)
                && message.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsVetoed(RecipeTrigger? trigger, string? message)
        => IsVetoed(trigger, message, null);

    public static bool IsVetoed(RecipeTrigger? trigger, string? message, ILogger? logger)
    {
        if (trigger == null || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return trigger.NoneOf.Any(c => ConditionMatches(c, message, logger));
    }

    private static bool ConditionMatches(RecipeCondition condition, string message, ILogger? logger)
    {
        if (condition.AnyWordStart is { Count: > 0 } && MatchesWordStart(condition.AnyWordStart, message, logger))
        {
            return true;
        }

        if (condition.AnySubstring is { Count: > 0 }
            && condition.AnySubstring.Any(s => message.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (condition.StartsWith is { Count: > 0 } && MatchesStartsWith(condition.StartsWith, message, logger))
        {
            return true;
        }

        return false;
    }

    private static bool MatchesStartsWith(IReadOnlyList<string> terms, string message, ILogger? logger)
        => MatchesStartsWith(terms, message, logger, RegexTimeout);

    /// <summary>
    /// Matches the opening of a message against the startsWith terms. The trailing space a term may
    /// carry is the author's marker for "this is a whole word, not a stem": "wie " must veto "Wie?" and
    /// "Wie geht das", but never "Wiederholung". A literal space cannot express that - it needs a real
    /// word boundary, which is why such a term is compiled to ^term\b instead of a plain prefix compare.
    /// Terms WITHOUT a trailing space stay open stems ("zeig" keeps covering "zeige mir"), which the
    /// seeded recipes rely on, so the two forms must not be conflated. Both directions are widening-only
    /// against the previous plain prefix compare: everything that matched before still matches.
    /// Internal (not private) with an explicit budget for the same reason as MatchesWordStart.
    /// </summary>
    /// <param name="terms">startsWith terms of one condition, whole words or open stems.</param>
    /// <param name="message">The user message; leading whitespace is ignored.</param>
    /// <param name="logger">Receives the timeout warning; may be null.</param>
    /// <param name="timeout">Wall-clock budget for the regex.</param>
    internal static bool MatchesStartsWith(
        IReadOnlyList<string> terms, string message, ILogger? logger, TimeSpan timeout)
    {
        var trimmed = message.TrimStart();
        var wholeWords = new List<string>();

        foreach (var term in terms)
        {
            var word = term.TrimEnd();

            // A term is a whole word only if it was written with a trailing space AND ends in a word
            // character - otherwise \b would assert a boundary BEFORE a word character and invert the
            // intent. Everything else keeps the plain prefix semantics.
            if (word.Length < term.Length && word.Length > 0 && char.IsLetterOrDigit(word[^1]))
            {
                wholeWords.Add(word);
            }
            else if (trimmed.StartsWith(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (wholeWords.Count == 0)
        {
            return false;
        }

        var pattern = "^(?:" + string.Join('|', wholeWords.Select(Regex.Escape)) + @")\b";
        try
        {
            return Regex.IsMatch(trimmed, pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);
        }
        catch (RegexMatchTimeoutException)
        {
            logger?.LogWarning(TimeoutMessage, timeout, message.Length);
            return false;
        }
    }

    private static bool MatchesWordStart(IReadOnlyList<string> stems, string message, ILogger? logger)
        => MatchesWordStart(stems, message, logger, RegexTimeout);

    // Internal (not private) with an explicit budget: the stems are Regex.Escape'd literals in a plain
    // alternation, so no input can force a timeout — only machine load can. RecipeTriggerMatcherTimeout
    // Tests therefore pass a budget small enough to trip deterministically, which is the only way to
    // prove the containment rather than assume it.
    internal static bool MatchesWordStart(
        IReadOnlyList<string> stems, string message, ILogger? logger, TimeSpan timeout)
    {
        var pattern = @"\b(?:" + string.Join('|', stems.Select(Regex.Escape)) + ")";
        try
        {
            return Regex.IsMatch(message, pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);
        }
        catch (RegexMatchTimeoutException)
        {
            logger?.LogWarning(TimeoutMessage, timeout, message.Length);
            return false;
        }
    }
}
