// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// DB-side group search used by Klacksy's search_and_navigate skill.
/// Replaces the previous full-table load + in-memory filter. When the substring filter finds
/// nothing, a fuzzy fallback ranks the term against the real group names in memory
/// (NameResolution: compact, fuzzy and phonetic stages) so spoken or decorated names still
/// resolve. With a root scope both paths look only at the groups of those subtrees, before the count and the cap.
/// </summary>
/// <param name="context">EF Core database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Models.Associations;
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

    public Task<GroupSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return SearchWithinRootsAsync(searchTerm, limit, null, cancellationToken);
    }

    public async Task<GroupSearchResult> SearchWithinRootsAsync(
        string? searchTerm,
        int limit,
        IReadOnlyCollection<Guid>? visibleRootIds,
        CancellationToken cancellationToken = default)
    {
        if (limit > MaxLimit) limit = MaxLimit;

        var query = GroupsInScope(visibleRootIds);

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
                Name = g.Name,
                Root = g.Root
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(searchTerm))
        {
            var fuzzyItems = await SearchFuzzyAsync(searchTerm, limit, visibleRootIds, cancellationToken);
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

    private IQueryable<Group> GroupsInScope(IReadOnlyCollection<Guid>? visibleRootIds)
    {
        var query = _context.Group
            .Where(g => !g.IsDeleted)
            .AsNoTracking();

        if (visibleRootIds is null)
        {
            return query;
        }

        var rootIds = visibleRootIds.ToList();
        return query.Where(g => rootIds.Contains(g.Root ?? g.Id));
    }

    private async Task<List<GroupSearchItem>> SearchFuzzyAsync(
        string searchTerm, int limit, IReadOnlyCollection<Guid>? visibleRootIds, CancellationToken cancellationToken)
    {
        var candidates = await GroupsInScope(visibleRootIds)
            .OrderBy(g => g.Name)
            .Take(FuzzyCandidateCap)
            .Select(g => new GroupSearchItem { Id = g.Id, Name = g.Name, Root = g.Root })
            .ToListAsync(cancellationToken);

        var resolution = NameResolution.Resolve(
            candidates, item => item.Name ?? string.Empty, searchTerm, GroupResolver.LabelWords, NameMatchPolicy.Lenient);
        if (resolution.Match != null)
        {
            return [resolution.Match];
        }

        return resolution.Candidates.Take(limit).ToList();
    }
}
