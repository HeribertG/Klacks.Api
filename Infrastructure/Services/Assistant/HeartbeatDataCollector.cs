// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Collects current data points from the database that are relevant for heartbeat status.
/// Counts new entries since a specific point in time in relevant tables.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class HeartbeatDataCollector : IHeartbeatDataCollector
{
    private readonly DataBaseContext _context;

    public HeartbeatDataCollector(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<HeartbeatDataSnapshot> CollectAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newAbsenceRequests = await _context.AbsenceDetail
            .CountAsync(a => !a.IsDeleted && a.CreateTime >= since, cancellationToken);

        var workEntriesCreatedToday = await _context.Work
            .CountAsync(w => !w.IsDeleted && w.CurrentDate == today, cancellationToken);

        var newScheduleChanges = await _context.ScheduleChange
            .CountAsync(sc => !sc.IsDeleted && sc.CreateTime >= since, cancellationToken);

        return new HeartbeatDataSnapshot(
            NewAbsenceRequests: newAbsenceRequests,
            WorkEntriesCreatedToday: workEntriesCreatedToday,
            NewScheduleChanges: newScheduleChanges,
            Since: since);
    }
}
