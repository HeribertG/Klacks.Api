// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns the flat skill_phrase row set into one IndexPhraseSet per owner, reproducing byte for byte
/// the phrase order and deduplication that the embedding text used to get from the jsonb columns.
/// Any change in here re-hashes and re-embeds every knowledge index entry, so the rules below are
/// not stylistic - they are the contract.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public static class SkillPhraseGrouper
{
    public static IReadOnlyDictionary<(string OwnerKind, string OwnerName), IndexPhraseSet> Group(
        IReadOnlyList<SkillPhrase> phrases)
    {
        var result = new Dictionary<(string OwnerKind, string OwnerName), IndexPhraseSet>();

        foreach (var owner in phrases.GroupBy(p => (p.OwnerKind, p.OwnerName)))
        {
            result[owner.Key] = new IndexPhraseSet(BuildKeywords(owner), BuildSynonyms(owner));
        }

        return result;
    }

    // Keywords are emitted RAW: no Distinct and no blank filter.
    //
    // Skill keywords come from a flat list that was never deduplicated on the read side, so a
    // duplicate in that list is part of the text and removing it would change the hash.
    //
    // Recipe keywords look deduplicated because they ARE - the extraction that produced them ended in
    // Distinct(OrdinalIgnoreCase) before they were stored. Deduplicating again here would be
    // idempotent today and therefore invisible, which is exactly why it must not be added: it would
    // silently start removing phrases as soon as a second source writes to the table.
    //
    // Ordering follows the same rule as the synonyms because keywords now carry a language too, and
    // SortOrder alone restarts at zero per language group. "mul" sorts FIRST: those are the
    // internationally used terms - smtp, imap, sso - and putting them at the head keeps them inside
    // the tokenizer's 512-token window on the long entries that get truncated. "und" sorts last, so
    // the phrases nobody has classified yet are the first to fall off.
    //
    // The rank comes from SkillPhraseLanguages rather than the UTF-8 byte count of the tag, which is
    // what this used before. The byte count produced the identical order only as long as neutral
    // rows were stored as null (zero bytes); with "mul" in the column it would sort them behind
    // every two-letter tag and truncate away exactly the terms the rule exists to protect.
    private static List<string> BuildKeywords(IEnumerable<SkillPhrase> ownerPhrases)
    {
        return ownerPhrases
            .Where(p => p.Kind == SkillPhraseKinds.Keyword)
            .OrderBy(p => SkillPhraseLanguages.OrderRank(p.Language))
            .ThenBy(p => p.Language, StringComparer.Ordinal)
            .ThenBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .ToList();
    }

    // Synonyms are deduplicated ACROSS languages, first occurrence wins. The same phrase listed under
    // "de" and under "en" appears exactly once in the text, in the position of the language that
    // sorts first. Sorting therefore has to happen before deduplication, never after.
    //
    // Real language tags keep the key order PostgreSQL uses inside a jsonb object, which is what the
    // dictionary deserialized from the jsonb column enumerated in: bytewise by tag. Ordinal
    // comparison equals a bytewise UTF-8 comparison for the ASCII language tags in use. All tags in
    // this group are two bytes, so ranking them together and comparing ordinally is the same order
    // the byte count produced.
    // A reserved tag on a synonym does not occur today; mul would lead, und would trail.
    private static List<string> BuildSynonyms(IEnumerable<SkillPhrase> ownerPhrases)
    {
        return ownerPhrases
            .Where(p => p.Kind == SkillPhraseKinds.Synonym)
            .Where(p => !string.IsNullOrWhiteSpace(p.Phrase))
            .OrderBy(p => SkillPhraseLanguages.OrderRank(p.Language))
            .ThenBy(p => p.Language, StringComparer.Ordinal)
            .ThenBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
