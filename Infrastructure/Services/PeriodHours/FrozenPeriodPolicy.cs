// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Database implementation of IFrozenPeriodPolicy: one query loads the distinct export ranges of the requested clients
/// (all of them, because the window depends on the person's latest ranges, not only on the ones overlapping the period),
/// the window rule (ExportedPeriodWindow) narrows the clients in memory, and one query checks the remaining clients for a
/// Closed real-plan work or break inside the period.
/// </summary>
/// <param name="context">Database context of the request scope</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.PeriodHours;

public class FrozenPeriodPolicy : IFrozenPeriodPolicy
{
    private static readonly IReadOnlySet<Guid> NoClients = new HashSet<Guid>();

    private readonly DataBaseContext _context;

    public FrozenPeriodPolicy(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlySet<Guid>> GetFrozenClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken)
    {
        if (analyseToken.HasValue || clientIds.Count == 0)
        {
            return NoClients;
        }

        var requestedIds = clientIds.Distinct().ToList();
        var exportRanges = await _context.ExportLogItem
            .AsNoTracking()
            .Where(item => !item.IsDeleted && requestedIds.Contains(item.ClientId))
            .Select(item => new { item.ClientId, item.StartDate, item.EndDate })
            .Distinct()
            .ToListAsync();

        var candidateIds = exportRanges
            .GroupBy(range => range.ClientId)
            .Where(ranges => ExportedPeriodWindow.IsBehindWindow(
                ranges.Select(range => (range.StartDate, range.EndDate)), startDate, endDate))
            .Select(ranges => ranges.Key)
            .ToList();

        if (candidateIds.Count == 0)
        {
            return NoClients;
        }

        var sealedIds = await _context.Work
            .Where(work => !work.IsDeleted
                && work.AnalyseToken == null
                && work.LockLevel == WorkLockLevel.Closed
                && work.CurrentDate >= startDate
                && work.CurrentDate <= endDate
                && candidateIds.Contains(work.ClientId))
            .Select(work => work.ClientId)
            .Concat(_context.Break
                .Where(entry => !entry.IsDeleted
                    && entry.AnalyseToken == null
                    && entry.LockLevel == WorkLockLevel.Closed
                    && entry.CurrentDate >= startDate
                    && entry.CurrentDate <= endDate
                    && candidateIds.Contains(entry.ClientId))
                .Select(entry => entry.ClientId))
            .Distinct()
            .ToListAsync();

        return sealedIds.ToHashSet();
    }

    public async Task<IReadOnlyList<ClientPeriodHours>> ExcludeFrozenRowsAsync(IReadOnlyCollection<ClientPeriodHours> rows)
    {
        var kept = new List<ClientPeriodHours>(rows.Count);
        foreach (var period in rows.GroupBy(row => (row.StartDate, row.EndDate, row.AnalyseToken)))
        {
            var frozen = await GetFrozenClientIdsAsync(
                period.Select(row => row.ClientId).ToList(),
                period.Key.StartDate,
                period.Key.EndDate,
                period.Key.AnalyseToken);
            kept.AddRange(period.Where(row => !frozen.Contains(row.ClientId)));
        }

        return kept;
    }
}
