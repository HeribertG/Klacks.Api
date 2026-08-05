// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Computes the placement fingerprint of a schedule window. Only rows a wizard may move are counted:
/// unlocked works and breaks that hang off no work. Locked works and sub-breaks belong to the sealed
/// part of the plan and never move, so they cannot invalidate a run.
/// </summary>
/// <param name="context">EF Core database context</param>
public sealed class ScheduleSnapshotMarkerService : IScheduleSnapshotMarkerService
{
    private readonly DataBaseContext _context;

    public ScheduleSnapshotMarkerService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<ScheduleSnapshotMarker> ComputeAsync(
        DateOnly from,
        DateOnly until,
        IReadOnlyList<Guid> agentIds,
        Guid? analyseToken,
        CancellationToken ct = default)
    {
        var agentSet = agentIds.ToList();

        var works = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == analyseToken
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && !w.IsDeleted
                        && w.LockLevel == WorkLockLevel.None
                        && agentSet.Contains(w.ClientId))
            .Select(w => new { w.Id, w.ClientId, w.CurrentDate, w.ShiftId, w.StartTime, w.EndTime })
            .ToListAsync(ct);

        var breaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == analyseToken
                        && b.ParentWorkId == null
                        && b.CurrentDate >= from
                        && b.CurrentDate <= until
                        && !b.IsDeleted
                        && agentSet.Contains(b.ClientId))
            .Select(b => new { b.Id, b.ClientId, b.CurrentDate })
            .ToListAsync(ct);

        var builder = new StringBuilder();
        foreach (var w in works
            .OrderBy(w => w.ClientId).ThenBy(w => w.CurrentDate).ThenBy(w => w.StartTime).ThenBy(w => w.Id))
        {
            builder.Append(w.Id).Append('|')
                .Append(w.ClientId).Append('|')
                .Append(w.CurrentDate).Append('|')
                .Append(w.ShiftId).Append('|')
                .Append(w.StartTime).Append('|')
                .Append(w.EndTime).Append(';');
        }

        builder.Append("::");
        foreach (var b in breaks.OrderBy(b => b.ClientId).ThenBy(b => b.CurrentDate).ThenBy(b => b.Id))
        {
            builder.Append(b.Id).Append('|')
                .Append(b.ClientId).Append('|')
                .Append(b.CurrentDate).Append(';');
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));

        return new ScheduleSnapshotMarker(
            from,
            until,
            agentSet,
            analyseToken,
            works.Count,
            breaks.Count,
            hash);
    }
}
