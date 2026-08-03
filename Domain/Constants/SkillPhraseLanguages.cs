// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Reserved language keys for phrase groups, taken from ISO 639-2 so they can never collide with a
/// real two-letter language tag. Both are stored verbatim in every layer - the seed JSON, the jsonb
/// columns and skill_phrase.language - so that a phrase without a real language has exactly one
/// representation instead of the three it used to have (mul in JSON, empty string in transport,
/// null in the column).
/// Multiple marks a phrase written identically in every language (smtp, imap, sso) and is the only
/// group the keyword matcher accepts as an anchor, which lets it match at two characters on a word
/// boundary. Undetermined marks a phrase whose language has not been assigned: recipe trigger stems,
/// which come from a DSL with no language dimension, and administrator input. Keeping the two apart
/// is what stops a three-letter stem like "add" from becoming a globally matching anchor.
/// </summary>
public static class SkillPhraseLanguages
{
    public const string Multiple = "mul";

    public const string Undetermined = "und";

    private const int MultipleRank = 0;
    private const int RealLanguageRank = 1;
    private const int UndeterminedRank = 2;

    /// <summary>
    /// Position of a language group inside the index text. Multiple sorts first so internationally
    /// used terms stay inside the tokenizer's sequence cap on long entries; Undetermined sorts last
    /// so unclassified phrases are the first to be truncated away. This reproduces the order the
    /// grouper previously derived from the UTF-8 byte count of the tag, which only worked while
    /// language-neutral rows were stored as null.
    /// </summary>
    /// <param name="language">Language tag of the phrase group</param>
    public static int OrderRank(string? language) => language switch
    {
        Multiple => MultipleRank,
        Undetermined => UndeterminedRank,
        _ => RealLanguageRank
    };

    /// <summary>
    /// Whether the tag denotes a phrase that belongs to no single language.
    /// </summary>
    /// <param name="language">Language tag of the phrase group</param>
    public static bool IsReserved(string? language) =>
        language is Multiple or Undetermined;
}
