// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillPhraseRepository : ISkillPhraseRepository
{
    private const string UniqueViolationSqlState = "23505";

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

    public async Task<IReadOnlyList<SkillPhrase>> GetActiveBySourceAsync(
        string source, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .AsNoTracking()
            .Where(p => p.Source == source && p.Status == SkillPhraseStatuses.Active)
            .OrderByDescending(p => p.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillPhrase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .FirstOrDefaultAsync(p => p.Id == id && p.Source == SkillPhraseSources.Learned, cancellationToken);
    }

    // The three single-row members are scoped to Learned on purpose. Their only caller is the learning card,
    // and an id alone would let it rewrite or reject a seed, language-pack or admin phrase - rows the
    // seed loaders own and would silently fight over on the next startup. A miss is reported as "not
    // found", which the controller turns into 404: an id outside the card's scope does not exist for it.
    // The scope has to sit on the lookup as well, not only on the writes: the update handler uses
    // GetByIdAsync purely to decide which store an id belongs to, so an unfiltered read would send a seed
    // id into the phrase branch and turn the rejected write into a "duplicate wording" answer.
    public async Task<bool> TryUpdatePhraseTextAsync(
        Guid id, string phrase, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SkillPhrases
            .FirstOrDefaultAsync(p => p.Id == id && p.Source == SkillPhraseSources.Learned, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        existing.Phrase = phrase;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(existing).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<SkillPhrase>> GetRejectedAsync(
        string ownerKind, string language, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .AsNoTracking()
            .Where(p => p.OwnerKind == ownerKind
                && p.Language == language
                && p.Status == SkillPhraseStatuses.Rejected)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var affected = await _context.SkillPhrases
            .Where(p => p.Id == id && p.Source == SkillPhraseSources.Learned)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, status), cancellationToken);

        return affected > 0;
    }

    // The learning loop writes exactly one row and must not touch its neighbours: the replace paths below
    // rewrite a whole (owner, language, kind, source) slice and remove every row the new list does not
    // carry, which would drop every phrase earlier rounds
    // learned for the same skill. SortOrder is appended rather than recomputed for the same reason.
    public async Task<Guid?> TryAddLearnedAsync(
        string ownerKind,
        string ownerName,
        string language,
        string kind,
        string phrase,
        CancellationToken cancellationToken = default)
    {
        var sortOrder = await _context.SkillPhrases
            .Where(p => p.OwnerKind == ownerKind
                && p.OwnerName == ownerName
                && p.Language == language
                && p.Kind == kind)
            .CountAsync(cancellationToken);

        var row = new SkillPhrase
        {
            Id = Guid.NewGuid(),
            OwnerKind = ownerKind,
            OwnerName = ownerName,
            Language = language,
            Kind = kind,
            Phrase = phrase,
            SortOrder = sortOrder,
            Source = SkillPhraseSources.Learned,
            Status = SkillPhraseStatuses.Active
        };

        await _context.SkillPhrases.AddAsync(row, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return row.Id;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(row).State = EntityState.Detached;
            return null;
        }
        catch
        {
            _context.Entry(row).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetPhraseTextsAsync(
        string ownerKind,
        string ownerName,
        string language,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .AsNoTracking()
            .Where(p => p.OwnerKind == ownerKind
                && p.OwnerName == ownerName
                && p.Language == language
                && p.Status == SkillPhraseStatuses.Active)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;

    public async Task<IReadOnlyList<string>> GetPhraseTextsBySourceAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        string language,
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillPhrases
            .AsNoTracking()
            .Where(p => p.OwnerKind == ownerKind
                && p.OwnerName == ownerName
                && p.Kind == kind
                && p.Source == source
                && p.Language == language)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.Phrase)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForLanguageAsync(
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        string language,
        IReadOnlyList<string> phrases,
        SkillPhraseReplaceScope scope = SkillPhraseReplaceScope.SameSourceOnly,
        CancellationToken cancellationToken = default)
    {
        var tag = NormalizeLanguage(language);
        var query = OwnerQuery(ownerKind, ownerName, kind, source, scope)
            .Where(p => p.Language == tag);

        var desired = new Dictionary<(string Language, string Phrase), int>();
        CollectDesiredPhrases(desired, kind, tag, phrases);

        await ApplyReplacementAsync(query, ownerKind, ownerName, kind, source, desired, cancellationToken);
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

        var desired = new Dictionary<(string Language, string Phrase), int>();
        if (phrasesByLanguage != null)
        {
            foreach (var entry in phrasesByLanguage)
            {
                CollectDesiredPhrases(desired, kind, NormalizeLanguage(entry.Key), entry.Value);
            }
        }

        await ApplyReplacementAsync(query, ownerKind, ownerName, kind, source, desired, cancellationToken);
    }

    // The startup seed loader calls ReplaceAllLanguagesAsync once per skill and kind on one scoped
    // context. Every saved phrase stayed tracked, so each further SaveChanges ran DetectChanges over
    // all phrases written so far - quadratic in the phrase count. With ~28k seed phrases on the
    // production database that made the 1.0.27 startup take over ten minutes at 100% CPU, longer
    // than the updater's health gate, and the auto-update rolled back (2026-09-10). Phrases are
    // write-only here, so detaching them after each save keeps the tracker at per-skill size.
    private void DetachTrackedPhrases()
    {
        foreach (var entry in _context.ChangeTracker.Entries<SkillPhrase>().ToList())
        {
            entry.State = EntityState.Detached;
        }
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

    // The replacement is a diff, not a delete-and-insert. Every seed version bump and every language
    // pack install rewrites its slice, and the soft delete turned each unchanged phrase into one more
    // tombstone per run - the table ended up holding more deleted than active rows. A row of the writing
    // source whose (language, phrase) is still wanted therefore stays the same row; only its position
    // follows the new list. A row of another source inside an AllSourcesOfOwner slice is still removed
    // and written anew: taking it over in place would carry its status along, and a rejected Learned
    // row turned into an Admin row would stay inactive where neither the learning card nor anyone
    // else can reach it.
    //
    // Status is deliberately not filtered: a rejected row still occupies the unique key. Listing its
    // wording again from the same source keeps the row as it is - neither a second row next to it nor a
    // silent reactivation - and a rejected wording the list no longer carries is removed like any other.
    //
    // Removal and insertion are still saved separately. The unique index is filtered on
    // is_deleted = false, so a row only clears its key once it is flagged as deleted; a single
    // SaveChanges would leave the statement order to the change tracker.
    private async Task ApplyReplacementAsync(
        IQueryable<SkillPhrase> query,
        string ownerKind,
        string ownerName,
        string kind,
        string source,
        IReadOnlyDictionary<(string Language, string Phrase), int> desired,
        CancellationToken cancellationToken)
    {
        var existing = await query.ToListAsync(cancellationToken);
        var kept = new Dictionary<(string Language, string Phrase), SkillPhrase>();
        var obsolete = new List<SkillPhrase>();

        foreach (var row in existing)
        {
            var key = (row.Language, row.Phrase);
            if (string.Equals(row.Source, source, StringComparison.Ordinal)
                && desired.ContainsKey(key)
                && kept.TryAdd(key, row))
            {
                continue;
            }

            obsolete.Add(row);
        }

        if (obsolete.Count > 0)
        {
            _context.SkillPhrases.RemoveRange(obsolete);
            await _context.SaveChangesAsync(cancellationToken);
        }

        foreach (var (key, sortOrder) in desired)
        {
            if (kept.TryGetValue(key, out var row))
            {
                if (row.SortOrder != sortOrder)
                {
                    row.SortOrder = sortOrder;
                }

                continue;
            }

            _context.SkillPhrases.Add(new SkillPhrase
            {
                Id = Guid.NewGuid(),
                OwnerKind = ownerKind,
                OwnerName = ownerName,
                Language = key.Language,
                Kind = kind,
                Phrase = key.Phrase,
                SortOrder = sortOrder,
                Source = source,
                Status = SkillPhraseStatuses.Active
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        DetachTrackedPhrases();
    }

    // Nothing reaches the column without a tag any more. A caller that has no language to give means
    // "not classified", which is Undetermined - never Multiple, because that one makes a short phrase an
    // anchor in the keyword matcher and would hand anchor status to whatever an unaudited caller happens
    // to pass. The slice is read under the same tag it is written under, so a repeated untagged call
    // finds the rows the previous one wrote.
    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? SkillPhraseLanguages.Undetermined : language;

    private static void CollectDesiredPhrases(
        Dictionary<(string Language, string Phrase), int> desired,
        string kind,
        string language,
        IReadOnlyList<string> phrases)
    {
        var keepBlanks = KeepsBlankPhrases(kind);

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
            // neither may fail because a definition happens to list a term twice. The first
            // position wins, as it always has.
            desired.TryAdd((language, phrase), sortOrder);
        }
    }

    // Mirrors the blank handling the index text has always had: synonyms are filtered, keywords are
    // not, because BuildEmbeddingText joins the keyword list unfiltered and dropping a blank there
    // would change the hash of every affected entry.
    private static bool KeepsBlankPhrases(string kind) => kind == SkillPhraseKinds.Keyword;
}
