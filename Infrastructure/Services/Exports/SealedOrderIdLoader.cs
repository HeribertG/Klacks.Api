// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the sealed orders owning closed work entries in a date range by walking the
/// Shift.OriginalId chain upward from the works' shifts, bounded by
/// ShiftConstants.MaxOriginalIdDescendantDepth with cycle protection.
/// @param fromDate - Lower bound (inclusive) for Work.CurrentDate
/// @param untilDate - Upper bound (inclusive) for Work.CurrentDate
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class SealedOrderIdLoader : ISealedOrderIdLoader
{
    private readonly DataBaseContext _context;

    public SealedOrderIdLoader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> LoadIdsForRangeAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken = default)
    {
        var shiftIds = await _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.Closed
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate)
            .Select(w => w.ShiftId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var sealedOrderIds = new HashSet<Guid>();
        var frontier = shiftIds.ToHashSet();
        var visited = new HashSet<Guid>(frontier);

        for (var depth = 0; depth <= ShiftConstants.MaxOriginalIdDescendantDepth && frontier.Count > 0; depth++)
        {
            var currentIds = frontier.ToList();
            var shifts = await _context.Shift
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.AnalyseToken == null && currentIds.Contains(s.Id))
                .Select(s => new { s.Id, s.OriginalId, s.Status })
                .ToListAsync(cancellationToken);

            frontier = [];
            foreach (var shift in shifts)
            {
                if (shift.Status == ShiftStatus.SealedOrder)
                {
                    sealedOrderIds.Add(shift.Id);
                }
                else if (shift.OriginalId.HasValue && visited.Add(shift.OriginalId.Value))
                {
                    frontier.Add(shift.OriginalId.Value);
                }
            }
        }

        return sealedOrderIds.ToList();
    }
}
