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
    /// Active phrases of one origin, ordered newest first. The learning card reads Learned with it.
    /// </summary>
    /// <param name="source">Origin to filter on, see SkillPhraseSources</param>
    Task<IReadOnlyList<SkillPhrase>> GetActiveBySourceAsync(string source, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// A single learned phrase, or null. Scoped to source Learned because its only caller uses it to tell
    /// the learning card's two id spaces apart; a seed or admin id must fall through to the other store.
    /// </summary>
    Task<SkillPhrase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rewrites the text of a single phrase and reports whether it stuck. Used by the admin card, which
    /// edits one row at a time and must not go through the replace-a-whole-language path the seed loaders
    /// use. Returns false when the new text already exists for the same owner, language and kind - the
    /// partial unique index rejects it, and a duplicate is a conflict to show, not an exception to throw.
    /// Only rows of source Learned are reachable; anything else reports not found.
    /// </summary>
    Task<bool> TryUpdatePhraseTextAsync(Guid id, string phrase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the review status of a single phrase. Rejected is how a phrase is withdrawn: the index
    /// synchroniser only reads Active rows, and the rejected row stays as a record that this phrase was
    /// tried and discarded. Only rows of source Learned are reachable; anything else reports not found.
    /// </summary>
    Task<bool> SetStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one learned phrase and reports whether it stuck. The learning loop writes a single row at a
    /// time and must never go through the replace-a-whole-language path, which would delete the phrases
    /// earlier rounds learned. A wording that already exists for the same owner, language and kind is
    /// rejected by the partial unique index; that is an answer ("this wording is already indexed, or was
    /// tried and rejected before"), not an exception.
    /// </summary>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="language">ISO tag of the phrase, or one of the reserved tags in SkillPhraseLanguages</param>
    Task<Guid?> TryAddLearnedAsync(
        string ownerKind,
        string ownerName,
        string language,
        string kind,
        string phrase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active phrase texts of one owner in one language, shown to the generator so it does not propose a
    /// wording that is already indexed.
    /// </summary>
    Task<IReadOnlyList<string>> GetPhraseTextsAsync(
        string ownerKind,
        string ownerName,
        string language,
        int limit,
        CancellationToken cancellationToken = default);

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
