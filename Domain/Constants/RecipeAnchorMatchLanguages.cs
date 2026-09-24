// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Match mode of the language-pack recipe anchors (recipe-anchors.json, AgentRecipe.Anchors), chosen by
/// the language the anchor belongs to, not by the request language. SubstringMatchLanguages match a term
/// anywhere in the message (case-insensitive contains): Japanese, Chinese and Thai write no spaces between
/// words, and Arabic and Hebrew glue articles and prepositions to the front of a word, so a word boundary
/// is missing or unreliable there. Every other pack language matches a term only at the start of a word
/// (\b plus the escaped literal), the semantics of RecipeCondition.AnyWordStart. Korean stays word-start:
/// it separates words with spaces and its particles attach at the end, so the stem still opens the word.
/// Codes use manifest spelling; the set compares case-insensitively.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecipeAnchorMatchLanguages
{
    public static readonly IReadOnlySet<string> SubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ar", "he", "ja", "th", "zh-CN", "zh-TW" };
}
