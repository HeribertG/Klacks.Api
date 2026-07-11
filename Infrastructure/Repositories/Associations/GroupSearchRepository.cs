// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DB-side group search used by Klacksy's search_and_navigate skill.
/// Replaces the previous full-table load + in-memory filter. When the substring filter finds
/// nothing, a fuzzy fallback ranks the term against the real group names in memory
/// (NameResolution: compact, fuzzy and phonetic stages) so spoken or decorated names still
/// resolve.
/// </summary>
/// <param name="context">EF Core database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupSearchRepository : IGroupSearchRepository
{
    private const int MaxLimit = 100;
    private const int FuzzyCandidateCap = 500;

    private readonly DataBaseContext _context;

    public GroupSearchRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<GroupSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit > MaxLimit) limit = MaxLimit;

        var query = _context.Group
            .Where(g => !g.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(g => g.Name)
            .Take(limit)
            .Select(g => new GroupSearchItem
            {
                Id = g.Id,
                Name = g.Name
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(searchTerm))
        {
            var fuzzyItems = await SearchFuzzyAsync(searchTerm, limit, cancellationToken);
            if (fuzzyItems.Count > 0)
            {
                return new GroupSearchResult
                {
                    Items = fuzzyItems,
                    TotalCount = fuzzyItems.Count
                };
            }
        }

        return new GroupSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    private async Task<List<GroupSearchItem>> SearchFuzzyAsync(
        string searchTerm, int limit, CancellationToken cancellationToken)
    {
        var candidates = await _context.Group
            .Where(g => !g.IsDeleted)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Take(FuzzyCandidateCap)
            .Select(g => new GroupSearchItem { Id = g.Id, Name = g.Name })
            .ToListAsync(cancellationToken);

        var resolution = NameResolution.Resolve(candidates, item => item.Name ?? string.Empty, searchTerm, GroupResolver.LabelWords);
        if (resolution.Match != null)
        {
            return [resolution.Match];
        }

        return resolution.Candidates.Take(limit).ToList();
    }
}
