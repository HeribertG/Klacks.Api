// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DB-side shift search by client name used by Klacksy's search_and_navigate skill.
/// Replaces the previous full-table load + in-memory filter.
/// </summary>
/// <param name="context">EF Core database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ShiftSearchRepository : IShiftSearchRepository
{
    private const int MaxLimit = 100;

    private readonly DataBaseContext _context;

    public ShiftSearchRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<ShiftSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit > MaxLimit) limit = MaxLimit;

        var query = _context.Shift
            .Include(s => s.Client)
            .Where(s => !s.IsDeleted && s.Client != null)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                (s.Client!.FirstName != null && s.Client.FirstName.ToLower().Contains(term)) ||
                (s.Client.Name != null && s.Client.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.FromDate)
            .Take(limit)
            .Select(s => new ShiftSearchItem
            {
                Id = s.Id,
                FromDate = s.FromDate,
                ClientFirstName = s.Client!.FirstName,
                ClientLastName = s.Client.Name
            })
            .ToListAsync(cancellationToken);

        return new ShiftSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
