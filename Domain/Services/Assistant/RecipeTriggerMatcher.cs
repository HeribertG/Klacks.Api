// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Matches a chat message against a recipe trigger. A trigger fires when every condition in allOf
/// matches AND no condition in noneOf matches. A single condition matches when any of its present
/// lists hits: anyWordStart (a stem at a word boundary, so mid-word false friends like "Pflege"⊃"lege"
/// do not trigger), anySubstring (case-insensitive contains), or startsWith (an opening of the
/// message; a term written WITH a trailing space is matched as a whole word, a term without one as an
/// open stem - see MatchesStartsWith).
/// Plugin-language synonyms (passed in for the request language) act as a whole-recipe OR shortcut:
/// when the structured allOf does not match, any synonym appearing as a substring fires the recipe,
/// still subject to the same noneOf guard. IsVetoed exposes the noneOf check on its own so the
/// semantic fallback can honor a recipe's exclusion vocabulary as well.
/// Every `language` parameter here is the request language, i.e. the caller's UI language (any of the
/// 25 supported languages). It is NOT a detected message language: a German UI may send a Spanish
/// message, which is why HasSemanticAnchor evaluates the pack anchors of all installed languages.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeTriggerMatcher
{
    // Wall-clock, not CPU time: the pattern itself is a plain alternation with no backtracking trap,
    // so a match that exceeds this budget means the machine was busy, not that the input was hostile.
    // The former 100 ms tripped under nothing worse than a concurrent build and made the trigger
    // disjointness gate flaky. One second still stops a pathological pattern long before a user notices.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public const int MinRequiredAnchors = 1;

    private const int VerbConditionCount = 1;

    private const string TimeoutMessage =
        "Recipe trigger regex timed out after {Timeout} on a {Length}-character message; treating the " +
        "condition as no match. The turn continues without a recipe.";

    // Deliberately carries no optional `language`. Adding one made this 3-parameter overload the better
    // match for Matches(trigger, null, message): a null literal converts to both string? and
    // IReadOnlyCollection<string>?, and of two applicable candidates the one omitting no optional argument
    // wins. The message then bound to `language`, `message` stayed null, and the call returned false for
    // every trigger - silently, which disarmed the live regression guard for the 2026-07-16 company-rule
    // incident. Callers that need a language pass synonyms explicitly:
    // Matches(trigger, null, message, language).
    public static bool Matches(RecipeTrigger trigger, string? message)
        => Matches(trigger, null, message, null, null, null);

    public static bool Matches(RecipeTrigger trigger, IReadOnlyCollection<string>? synonyms, string? message, string? language = null)
        => Matches(trigger, synonyms, message, null, language, null);

    public static bool Matches(
        RecipeTrigger trigger, IReadOnlyCollection<string>? synonyms, string? message, ILogger? logger, string? language = null)
        => Matches(trigger, synonyms, message, logger, language, null);

    /// <summary>
    /// The full form. <paramref name="packVetoTerms"/> is the language pack's question-word veto for this
    /// recipe (AgentRecipe.VetoesFor(language)) and is REQUIRED here rather than optional. Every shorter
    /// overload above is unchanged and passes null, so no existing call site alters which overload it
    /// binds to. Adding an optional parameter to one of them instead would shift overload resolution for
    /// every positional call whose arguments fit more than one signature - which is exactly how the
    /// 2026-09-14 change silently disarmed the live regression guard for the 2026-07-16 incident: a
    /// null literal at position 2 bound to the new parameter, the message went null, and every match
    /// returned false while the guard asserting ShouldBeFalse stayed permanently satisfied.
    /// </summary>
    /// <param name="trigger">The structured allOf/noneOf trigger; may be null.</param>
    /// <param name="synonyms">Pack synonyms for the request language; whole-recipe OR shortcut.</param>
    /// <param name="message">The user message.</param>
    /// <param name="logger">Receives regex timeout warnings; may be null.</param>
    /// <param name="language">Request (UI) language, not necessarily the message language; null skips locale-bound conditions.</param>
    /// <param name="packVetoTerms">Pack question-word veto for this recipe and language; may be null.</param>
    public static bool Matches(
        RecipeTrigger trigger,
        IReadOnlyCollection<string>? synonyms,
        string? message,
        ILogger? logger,
        string? language,
        IReadOnlyCollection<string>? packVetoTerms)
    {
        if (trigger == null || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (IsVetoed(trigger, message, logger, language, packVetoTerms))
        {
            return false;
        }

        if (trigger.AllOf.Count > 0 && trigger.AllOf.All(c => ConditionMatches(c, message, logger, language)))
        {
            return true;
        }

        return synonyms is { Count: > 0 }
            && synonyms.Any(s => !string.IsNullOrWhiteSpace(s)
                && message.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsVetoed(RecipeTrigger? trigger, string? message, string? language = null)
        => IsVetoed(trigger, message, null, language, null);

    public static bool IsVetoed(RecipeTrigger? trigger, string? message, ILogger? logger, string? language = null)
        => IsVetoed(trigger, message, logger, language, null);

    /// <summary>
    /// The full veto check. The pack vocabulary is evaluated FIRST and independently of the trigger,
    /// because noneOf is core-language only: for a plugin-language message the structured veto has no
    /// surface to match, so the pack terms are the only thing standing between an information question
    /// and a mutation recipe. Terms carry the same startsWith semantics as noneOf[].startsWith.
    /// </summary>
    public static bool IsVetoed(
        RecipeTrigger? trigger,
        string? message,
        ILogger? logger,
        string? language,
        IReadOnlyCollection<string>? packVetoTerms)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (packVetoTerms is { Count: > 0 })
        {
            var terms = packVetoTerms as IReadOnlyList<string> ?? packVetoTerms.ToList();
            if (MatchesStartsWith(terms, message, logger))
            {
                return true;
            }
        }

        return trigger != null && trigger.NoneOf.Any(c => ConditionMatches(c, message, logger, language));
    }

    /// <summary>
    /// Number of non-verb allOf conditions the message hits. By recipe-authoring convention allOf[0] is
    /// the verb group and every later condition names the subject, so these are the conditions that
    /// distinguish one recipe from another.
    /// </summary>
    /// <param name="trigger">The structured allOf/noneOf trigger; may be null.</param>
    /// <param name="message">The user message.</param>
    /// <param name="logger">Receives regex timeout warnings; may be null.</param>
    /// <param name="language">Request (UI) language, not necessarily the message language; null skips locale-bound conditions.</param>
    public static int CountAnchors(RecipeTrigger? trigger, string? message, ILogger? logger = null, string? language = null)
    {
        if (trigger == null || string.IsNullOrWhiteSpace(message))
        {
            return 0;
        }

        return trigger.AllOf
            .Skip(VerbConditionCount)
            .Count(c => ConditionMatches(c, message, logger, language));
    }

    /// <summary>
    /// Lexical gate for the semantic fallback: an embedding hit only counts when the message also names
    /// the recipe's subject. Without it a topical neighbour ("read my deferred notes") ranks above the
    /// floor against a write recipe and reaches the confirmation gate. The subject counts as named when
    /// the core trigger has at least <see cref="MinRequiredAnchors"/> non-verb condition hits OR any
    /// installed language pack has an anchor term in the message. The pack side is the union over ALL
    /// installed languages, not only the request language, because the request language is the UI
    /// language and says nothing about the language the message is written in. Each pack term is matched
    /// by the mode of its own language (<see cref="RecipeAnchorMatchLanguages"/>).
    /// Returns true, meaning no restriction, when the trigger has no non-verb condition (a single
    /// phrase-list condition can never be missed by the message here, because the keyword path already
    /// ran and did not match), and for a request language outside the core set for which the recipe
    /// carries no pack anchors - there neither vocabulary exists, so gating would only cost recall.
    /// </summary>
    /// <param name="trigger">The structured allOf/noneOf trigger; may be null.</param>
    /// <param name="message">The user message.</param>
    /// <param name="logger">Receives regex timeout warnings; may be null.</param>
    /// <param name="language">Request (UI) language, not necessarily the message language; null counts as core.</param>
    /// <param name="packAnchors">The recipe's pack anchors keyed by language code (AgentRecipe.AllAnchors()); may be null.</param>
    public static bool HasSemanticAnchor(
        RecipeTrigger? trigger,
        string? message,
        ILogger? logger = null,
        string? language = null,
        IReadOnlyDictionary<string, List<string>>? packAnchors = null)
    {
        if (trigger == null || trigger.AllOf.Count <= VerbConditionCount || string.IsNullOrWhiteSpace(message))
        {
            return true;
        }

        if (IsOutsideCoreLanguages(language) && !HasPackAnchorsFor(packAnchors, language!))
        {
            return true;
        }

        if (CountAnchors(trigger, message, logger, language) >= MinRequiredAnchors)
        {
            return true;
        }

        return MatchesAnyPackAnchor(packAnchors, message, logger);
    }

    private static bool IsOutsideCoreLanguages(string? language) =>
        !string.IsNullOrEmpty(language)
        && !MultiLanguage.CoreLanguages.Contains(language, StringComparer.OrdinalIgnoreCase);

    private static bool HasPackAnchorsFor(IReadOnlyDictionary<string, List<string>>? packAnchors, string language) =>
        packAnchors != null
        && packAnchors.Any(entry => string.Equals(entry.Key, language, StringComparison.OrdinalIgnoreCase)
            && entry.Value is { Count: > 0 });

    private static bool MatchesAnyPackAnchor(
        IReadOnlyDictionary<string, List<string>>? packAnchors, string message, ILogger? logger)
    {
        if (packAnchors is not { Count: > 0 })
        {
            return false;
        }

        var wordStartTerms = new List<string>();
        foreach (var (anchorLanguage, terms) in packAnchors)
        {
            if (terms is not { Count: > 0 })
            {
                continue;
            }

            var usable = terms.Where(term => !string.IsNullOrWhiteSpace(term));
            if (RecipeAnchorMatchLanguages.SubstringMatchLanguages.Contains(anchorLanguage))
            {
                if (usable.Any(term => message.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            else
            {
                wordStartTerms.AddRange(usable);
            }
        }

        return wordStartTerms.Count > 0 && MatchesWordStart(wordStartTerms, message, logger);
    }

    private static bool ConditionMatches(RecipeCondition condition, string message, ILogger? logger, string? language)
    {
        if (condition.AnyWordStart is { Count: > 0 } && MatchesWordStart(condition.AnyWordStart, message, logger))
        {
            return true;
        }

        // AnyWordStartByLocale: per-locale stems that only fire for the request language.
        // Case-insensitive key lookup, matching SynonymsFor's convention.
        if (condition.AnyWordStartByLocale != null && language != null)
        {
            foreach (var kvp in condition.AnyWordStartByLocale)
            {
                if (string.Equals(kvp.Key, language, StringComparison.OrdinalIgnoreCase)
                    && kvp.Value is { Count: > 0 }
                    && MatchesWordStart(kvp.Value, message, logger))
                {
                    return true;
                }
            }
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
    /// The opening is taken after leading whitespace AND leading punctuation
    /// (<see cref="RecipeTriggerLeadingPunctuation"/>): "¿Cómo …?" must meet the veto "cómo " like
    /// "Cómo …?" does, and a quote, bracket or dash in front of a question word must not hide it.
    /// </summary>
    /// <param name="terms">startsWith terms of one condition, whole words or open stems.</param>
    /// <param name="message">The user message; leading whitespace and leading punctuation are ignored.</param>
    /// <param name="logger">Receives the timeout warning; may be null.</param>
    /// <param name="timeout">Wall-clock budget for the regex.</param>
    internal static bool MatchesStartsWith(
        IReadOnlyList<string> terms, string message, ILogger? logger, TimeSpan timeout)
    {
        var trimmed = SkipLeadingWhitespaceAndPunctuation(message);
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

    private static string SkipLeadingWhitespaceAndPunctuation(string message)
    {
        var start = 0;
        while (start < message.Length
               && (char.IsWhiteSpace(message[start])
                   || RecipeTriggerLeadingPunctuation.Characters.Contains(message[start])))
        {
            start++;
        }

        return message[start..];
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
