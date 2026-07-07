// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds a period-closed lookup for a date range by bulk-loading SealedDay rows with
/// Level=Closed and resolving group-scoped seals against non-deleted GroupItem memberships,
/// so callers can evaluate (client, date) pairs in memory without further queries.
/// @param fromDate - Lower bound (inclusive) for SealedDay.Date
/// @param untilDate - Upper bound (inclusive) for SealedDay.Date
/// @param clientIds - Clients whose group memberships are resolved for group-scoped seals
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PeriodClosedEntryFilter : IPeriodClosedEntryFilter
{
    private readonly DataBaseContext _context;

    public PeriodClosedEntryFilter(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IPeriodClosedLookup> BuildAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        var sealedDays = await _context.SealedDay
            .AsNoTracking()
            .Where(sd => !sd.IsDeleted
                && sd.Level == WorkLockLevel.Closed
                && sd.Date >= fromDate
                && sd.Date <= untilDate)
            .Select(sd => new { sd.Date, sd.GroupId })
            .ToListAsync(cancellationToken);

        var globallyClosedDates = sealedDays
            .Where(sd => sd.GroupId == null)
            .Select(sd => sd.Date)
            .ToHashSet();

        var groupScopedSeals = sealedDays
            .Where(sd => sd.GroupId != null)
            .GroupBy(sd => sd.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(sd => sd.Date).ToList());

        var closedClientDates = new HashSet<(Guid ClientId, DateOnly Date)>();

        if (groupScopedSeals.Count > 0 && clientIds.Count > 0)
        {
            var sealedGroupIds = groupScopedSeals.Keys.ToList();

            var memberships = await _context.GroupItem
                .AsNoTracking()
                .Where(gi => !gi.IsDeleted
                    && gi.AnalyseToken == null
                    && gi.ClientId != null
                    && clientIds.Contains(gi.ClientId.Value)
                    && sealedGroupIds.Contains(gi.GroupId))
                .Select(gi => new { ClientId = gi.ClientId!.Value, gi.GroupId })
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var membership in memberships)
            {
                foreach (var date in groupScopedSeals[membership.GroupId])
                {
                    closedClientDates.Add((membership.ClientId, date));
                }
            }
        }

        return new PeriodClosedLookup(globallyClosedDates, closedClientDates);
    }
}
