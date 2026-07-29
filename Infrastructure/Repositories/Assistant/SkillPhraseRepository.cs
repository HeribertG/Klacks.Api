// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillPhrase entities. Reads the whole active phrase set in a single query
/// because its only reader, the knowledge index synchronizer, walks every skill and recipe at once
/// and a per-owner query would turn one round trip into several hundred. Writes are replacements
/// scoped by origin.
/// </summary>
/// <param name="context">The database context for accessing the skill_phrase table</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillPhraseRepository : ISkillPhraseRepository
{
    private readonly DataBaseContext _context;

    public SkillPhraseRepository(DataBaseContext context)
    {
        _context = context;
    }

    // Soft-deleted rows are excluded by the query filter on SkillPhrase; only the review status has
    // to be filtered here. No ordering is applied: the order the index text needs depends on the
    // phrase kind and is established by SkillPhraseGrouper.
    public async Task<IReadOnlyList<SkillPhrase>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .AsNoTracking()
            .Where(p => p.Status == SkillPhraseStatuses.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForLanguageAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        string? language,
        IReadOnlyList<string> phrases,
        SkillPhraseReplaceScope scope = SkillPhraseReplaceScope.SameSourceOnly,
        CancellationToken cancellationToken = default)
    {
        var query = OwnerQuery(ownerKind, ownerName, kind, source, scope);

        query = language == null
            ? query.Where(p => p.Language == null)
            : query.Where(p => p.Language == language);

        await RemoveExistingAsync(query, cancellationToken);
        AddPhrases(ownerKind, ownerName, kind, source, language, phrases);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceAllLanguagesAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        IReadOnlyDictionary<string, List<string>>? phrasesByLanguage,
        SkillPhraseReplaceScope scope = SkillPhraseReplaceScope.SameSourceOnly,
        CancellationToken cancellationToken = default)
    {
        var query = OwnerQuery(ownerKind, ownerName, kind, source, scope);

        await RemoveExistingAsync(query, cancellationToken);

        if (phrasesByLanguage != null)
        {
            foreach (var entry in phrasesByLanguage)
            {
                AddPhrases(ownerKind, ownerName, kind, source, entry.Key, entry.Value);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // The source belongs into the delete filter, and that is the whole point of this repository.
    // navigation_target_synonyms carries the same kind of source column, but
    // NavigationTargetSynonymRepository.ReplaceForTargetLanguageAsync deletes on (TargetId, Language)
    // alone: one training call rewrote every row as "user", after which the seed refresh - which only
    // runs while all rows still read "seed" - refused to work for good. A replacement that ignores the
    // origin turns two independent writers into one destructive writer.
    //
    // AllSourcesOfOwner exists for the single case where the legacy jsonb value itself is overwritten
    // wholesale (the administrator meta-skills replace the complete dictionary). Keeping foreign rows
    // there would diverge from the jsonb value and collide with the unique index, which carries no
    // source column.
    private IQueryable<SkillPhrase> OwnerQuery(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        SkillPhraseReplaceScope scope)
    {
        var query = _context.SkillPhrases
            .Where(p => p.OwnerKind == ownerKind && p.OwnerName == ownerName && p.Kind == kind);

        return scope == SkillPhraseReplaceScope.SameSourceOnly
            ? query.Where(p => p.Source == source)
            : query;
    }

    // Removal and insertion are saved separately on purpose. The unique index is filtered on
    // is_deleted = false, so a re-inserted identical phrase only clears the old row once that row is
    // flagged as deleted; a single SaveChanges would leave the statement order to the change tracker.
    // Status is deliberately not filtered: a rejected row still occupies the unique key.
    private async Task RemoveExistingAsync(IQueryable<SkillPhrase> query, CancellationToken cancellationToken)
    {
        var existing = await query.ToListAsync(cancellationToken);
        if (existing.Count == 0)
        {
            return;
        }

        _context.SkillPhrases.RemoveRange(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private void AddPhrases(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        string? language,
        IReadOnlyList<string> phrases)
    {
        var keepBlanks = KeepsBlankPhrases(kind);
        var written = new HashSet<string>(StringComparer.Ordinal);

        for (var sortOrder = 0; sortOrder < phrases.Count; sortOrder++)
        {
            var phrase = phrases[sortOrder];

            if (!keepBlanks && string.IsNullOrWhiteSpace(phrase))
            {
                continue;
            }

            // The unique index spans (owner, language, kind, phrase) without a source column, so an
            // exact repeat inside one list cannot be stored twice. It is skipped rather than thrown
            // on: the callers are the startup seed loader and the language pack installer, and
            // neither may fail because a definition happens to list a term twice.
            if (!written.Add(phrase))
            {
                continue;
            }

            _context.SkillPhrases.Add(new SkillPhrase
            {
                Id = Guid.NewGuid(),
                OwnerKind = ownerKind,
                OwnerName = ownerName,
                // A dictionary cannot carry a null key, so callers that mean "no language" pass an
                // empty string. Both must reach the column as null, which is its documented value for
                // a phrase that applies to every language.
                Language = string.IsNullOrEmpty(language) ? null : language,
                Kind = kind,
                Phrase = phrase,
                SortOrder = sortOrder,
                Source = source,
                Status = SkillPhraseStatuses.Active
            });
        }
    }

    // Mirrors the blank handling the index text has always had: synonyms are filtered, keywords are
    // not, because BuildEmbeddingText joins the keyword list unfiltered and dropping a blank there
    // would change the hash of every affected entry.
    private static bool KeepsBlankPhrases(string kind) => kind == SkillPhraseKinds.Keyword;
}
