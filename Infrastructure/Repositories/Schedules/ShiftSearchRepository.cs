// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DB-side shift search by shift abbreviation/name or linked client name, used by Klacksy's
/// search_and_navigate skill. Shifts without a client (group-only shifts) are searchable too.
/// Only OriginalShift/SplitShift are matched — OriginalOrder/SealedOrder are order instances of
/// a shift, not the shift definition itself, and would otherwise clutter results with ambiguous
/// duplicates that share the same name/abbreviation as their originating shift.
/// </summary>
/// <param name="context">EF Core database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
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
            .Where(s => !s.IsDeleted && (s.Status == ShiftStatus.OriginalShift || s.Status == ShiftStatus.SplitShift))
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.ToLower().Replace("(", " ").Replace(")", " ").Replace(",", " ");
            var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                query = query.Where(s =>
                    s.Abbreviation.ToLower().Contains(token) ||
                    s.Name.ToLower().Contains(token) ||
                    (s.Client != null && s.Client.FirstName != null && s.Client.FirstName.ToLower().Contains(token)) ||
                    (s.Client != null && s.Client.Name != null && s.Client.Name.ToLower().Contains(token)));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.FromDate)
            .Take(limit)
            .Select(s => new ShiftSearchItem
            {
                Id = s.Id,
                FromDate = s.FromDate,
                Abbreviation = s.Abbreviation,
                Name = s.Name,
                Status = s.Status,
                ClientFirstName = s.Client != null ? s.Client.FirstName : null,
                ClientLastName = s.Client != null ? s.Client.Name : null
            })
            .ToListAsync(cancellationToken);

        return new ShiftSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
