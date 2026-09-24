// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Match mode of the language-pack recipe anchors (recipe-anchors.json, AgentRecipe.Anchors), chosen by
/// the language the anchor belongs to, not by the request language. SubstringMatchLanguages match a term
/// anywhere in the message (case-insensitive contains): Japanese, Chinese and Thai write no spaces between
/// words, and Arabic and Hebrew glue articles and prepositions to the front of a word, so a word boundary
/// is missing or unreliable there. Danish, Finnish and Swedish match as substring too: they write compounds
/// as one word with the head noun at the end ("systemadgang", "passorder", "pyhäkalenteri"), so a word-start
/// term misses exactly the noun the anchor names; measured on the pack sentences this recovered recall
/// without extra collisions on core turns. WordStartMatchLanguages match a term only at the start of a word
/// (\b plus the escaped literal), the semantics of RecipeCondition.AnyWordStart. Korean stays word-start:
/// it separates words with spaces and its particles attach at the end, so the stem still opens the word.
/// Every pack language must be named in exactly one set (RecipeAnchorMatchLanguagesGuardTests); the matcher
/// treats a code outside SubstringMatchLanguages as word-start, so a new script without word boundaries
/// (e.g. km, my) must be added to SubstringMatchLanguages consciously.
/// Codes use manifest spelling; the sets compare case-insensitively.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecipeAnchorMatchLanguages
{
    public static readonly IReadOnlySet<string> SubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "da", "fi", "he", "ja", "sv", "th", "zh-CN", "zh-TW"
        };

    public static readonly IReadOnlySet<string> WordStartMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cs", "el", "es", "id", "ko", "ms", "nb", "nl", "pl", "pt", "ro", "vi"
        };
}
