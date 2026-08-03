// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the trigger phrases of skills and recipes stored in skill_phrase.
/// Every write is a replacement that carries the origin of the phrases, so one writer can never
/// silently discard what another writer contributed.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillPhraseRepository
{
    Task<IReadOnlyList<SkillPhrase>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the phrases of one owner for exactly one language. By default only rows of the given
    /// source are removed.
    /// </summary>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="kind">Synonym or Keyword, see SkillPhraseKinds</param>
    /// <param name="source">Origin written onto the new rows, see SkillPhraseSources</param>
    /// <param name="language">Language of the phrases: an ISO tag, or one of the reserved tags in SkillPhraseLanguages when the phrases belong to no single language</param>
    /// <param name="phrases">The new phrases; the position in this list becomes SortOrder</param>
    /// <param name="scope">Whether the removal stays inside the given source (default) or covers every source</param>
    Task ReplaceForLanguageAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        string language,
        IReadOnlyList<string> phrases,
        SkillPhraseReplaceScope scope = SkillPhraseReplaceScope.SameSourceOnly,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the phrases of one owner across every language at once. Used where the caller owns the
    /// complete per-language contribution of one source, such as a seed definition whose synonym
    /// dictionary is the entire seed contribution for that skill.
    /// </summary>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="kind">Synonym or Keyword, see SkillPhraseKinds</param>
    /// <param name="source">Origin written onto the new rows, see SkillPhraseSources</param>
    /// <param name="phrasesByLanguage">New phrases per language code; null or empty clears the source</param>
    /// <param name="scope">Whether the removal stays inside the given source (default) or covers every source</param>
    Task ReplaceAllLanguagesAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        IReadOnlyDictionary<string, List<string>>? phrasesByLanguage,
        SkillPhraseReplaceScope scope = SkillPhraseReplaceScope.SameSourceOnly,
        CancellationToken cancellationToken = default);
}
