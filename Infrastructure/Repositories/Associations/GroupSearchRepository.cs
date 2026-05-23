// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DB-side group search used by Klacksy's search_and_navigate skill.
/// Replaces the previous full-table load + in-memory filter.
/// </summary>
/// <param name="context">EF Core database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupSearchRepository : IGroupSearchRepository
{
    private const int MaxLimit = 100;

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

        return new GroupSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
