// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds a period-closed lookup for a date range from SealedDay rows with Level=Closed. A global seal closes every
/// client; a group seal closes a client exactly when DayLockAttribution says it locks them - the client worked a shift
/// of that group on that day (the group close seals works by the shift's group) or is an active member of the group on
/// that day (Membership and GroupItem validity windows). This is the same rule as the day lock and the payroll
/// completeness gate, so a Closed work of a non-member who worked a group shift is exported after the group close.
/// Callers evaluate (client, date) pairs in memory without further queries.
/// @param fromDate - Lower bound (inclusive) for SealedDay.Date
/// @param untilDate - Upper bound (inclusive) for SealedDay.Date
/// @param clientIds - Clients for which group-scoped seals are resolved
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
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

        var groupSealedDates = sealedDays
            .Where(sd => sd.GroupId != null && !globallyClosedDates.Contains(sd.Date))
            .Select(sd => sd.Date)
            .Distinct()
            .ToList();

        var closedClientDates = new HashSet<(Guid ClientId, DateOnly Date)>();

        if (groupSealedDates.Count > 0 && clientIds.Count > 0)
        {
            var pairs = groupSealedDates
                .SelectMany(date => clientIds.Distinct().Select(clientId => (date, clientId)))
                .ToList();

            var locking = await DayLockAttribution.LoadLockingSealsAsync(
                _context, pairs, null, cancellationToken, WorkLockLevel.Closed);

            foreach (var (date, clientId) in locking.Keys)
            {
                closedClientDates.Add((clientId, date));
            }
        }

        return new PeriodClosedLookup(globallyClosedDates, closedClientDates);
    }
}
