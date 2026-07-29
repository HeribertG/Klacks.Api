// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns the flat skill_phrase row set into one IndexPhraseSet per owner, reproducing byte for byte
/// the phrase order and deduplication that the embedding text used to get from the jsonb columns.
/// Any change in here re-hashes and re-embeds every knowledge index entry, so the rules below are
/// not stylistic - they are the contract.
/// </summary>
using System.Text;
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
    // SortOrder alone restarts at zero per language group. Language-less phrases sort FIRST (zero
    // bytes): those are the internationally used terms - smtp, token, import - and putting them at
    // the head keeps them inside the tokenizer's 512-token window on the long entries that get
    // truncated. "und" (three bytes) lands after the real language tags, so the phrases nobody has
    // classified yet are the first to fall off.
    private static List<string> BuildKeywords(IEnumerable<SkillPhrase> ownerPhrases)
    {
        return ownerPhrases
            .Where(p => p.Kind == SkillPhraseKinds.Keyword)
            .OrderBy(p => Encoding.UTF8.GetByteCount(p.Language ?? string.Empty))
            .ThenBy(p => p.Language ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .ToList();
    }

    // Synonyms are deduplicated ACROSS languages, first occurrence wins. The same phrase listed under
    // "de" and under "en" appears exactly once in the text, in the position of the language that
    // sorts first. Sorting therefore has to happen before deduplication, never after.
    //
    // The sort reproduces the key order PostgreSQL uses inside a jsonb object, which is what the
    // dictionary deserialized from the jsonb column enumerated in: shorter keys first, then bytewise.
    // Ordinal comparison equals a bytewise UTF-8 comparison for the ASCII language tags in use, and
    // GetByteCount rather than Length keeps that true for a multi-byte tag as well.
    // A synonym without a language does not occur today; it is sorted into the leading group.
    private static List<string> BuildSynonyms(IEnumerable<SkillPhrase> ownerPhrases)
    {
        return ownerPhrases
            .Where(p => p.Kind == SkillPhraseKinds.Synonym)
            .Where(p => !string.IsNullOrWhiteSpace(p.Phrase))
            .OrderBy(p => Encoding.UTF8.GetByteCount(p.Language ?? string.Empty))
            .ThenBy(p => p.Language ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
