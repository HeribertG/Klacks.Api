// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for NavigationTargetSynonym entities.
/// </summary>
/// <param name="context">The database context for accessing the navigation_target_synonyms table</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class NavigationTargetSynonymRepository : INavigationTargetSynonymRepository
{
    private readonly DataBaseContext _context;

    public NavigationTargetSynonymRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NavigationTargetSynonym>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.NavigationTargetSynonyms
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NavigationTargetSynonym>> GetByLanguagesAsync(IEnumerable<string> languages, CancellationToken ct = default)
    {
        var langList = languages.ToList();
        return await _context.NavigationTargetSynonyms
            .Where(s => langList.Contains(s.Language))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<NavigationTargetSynonym> synonyms, CancellationToken ct = default)
    {
        await _context.NavigationTargetSynonyms.AddRangeAsync(synonyms, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ReplaceForTargetLanguageAsync(string targetId, string language, IEnumerable<string> keywords, string source, CancellationToken ct = default)
    {
        var existing = await _context.NavigationTargetSynonyms
            .IgnoreQueryFilters()
            .Where(s => s.TargetId == targetId && s.Language == language && !s.IsDeleted)
            .ToListAsync(ct);

        _context.NavigationTargetSynonyms.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var keyword in keywords)
        {
            _context.NavigationTargetSynonyms.Add(new NavigationTargetSynonym
            {
                Id = Guid.NewGuid(),
                TargetId = targetId,
                Language = language,
                Keyword = keyword,
                Source = source,
                CreateTime = now
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NavigationTargetSynonym>> GetActiveForTargetLanguageAsync(string targetId, string language, CancellationToken ct = default)
    {
        return await _context.NavigationTargetSynonyms
            .Where(s => s.TargetId == targetId && s.Language == language)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveEntriesForTargetLanguageAsync(string targetId, string language, CancellationToken ct = default)
    {
        return await _context.NavigationTargetSynonyms
            .AnyAsync(s => s.TargetId == targetId && s.Language == language, ct);
    }

    public async Task<NavigationTargetSynonymSyncResult> SyncSeedKeywordsForTargetLanguageAsync(string targetId, string language, IReadOnlyCollection<string> keywords, CancellationToken ct = default)
    {
        var existing = await _context.NavigationTargetSynonyms
            .Where(s => s.TargetId == targetId && s.Language == language)
            .ToListAsync(ct);

        var manifestKeywords = keywords
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        var manifestKeywordSet = new HashSet<string>(manifestKeywords, StringComparer.OrdinalIgnoreCase);

        var seedRows = existing
            .Where(e => string.Equals(e.Source, SynonymSources.Seed, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var foreignRows = existing
            .Where(e => !string.Equals(e.Source, SynonymSources.Seed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var seedRowsToRemove = seedRows
            .Where(e => !manifestKeywordSet.Contains(e.Keyword))
            .ToList();
        if (seedRowsToRemove.Count > 0)
        {
            _context.NavigationTargetSynonyms.RemoveRange(seedRowsToRemove);
        }

        var remainingKeywords = new HashSet<string>(
            existing.Except(seedRowsToRemove).Select(e => e.Keyword),
            StringComparer.OrdinalIgnoreCase);

        var keywordsToInsert = manifestKeywords
            .Where(k => !remainingKeywords.Contains(k))
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var keyword in keywordsToInsert)
        {
            _context.NavigationTargetSynonyms.Add(new NavigationTargetSynonym
            {
                Id = Guid.NewGuid(),
                TargetId = targetId,
                Language = language,
                Keyword = keyword,
                Source = SynonymSources.Seed,
                CreateTime = now
            });
        }

        if (seedRowsToRemove.Count > 0 || keywordsToInsert.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return new NavigationTargetSynonymSyncResult(keywordsToInsert.Count, seedRowsToRemove.Count, foreignRows.Count);
    }
}
