// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Data;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// PostgreSQL implementation of the snapshot guard. RepeatableRead maps to snapshot isolation there, so
/// the whole read phase sees the database as of its first statement. The fingerprint deliberately keeps
/// the soft-delete filter: a delete changes the count, an edit changes the newest timestamp, and a
/// delete followed by a recreate changes the timestamp even when the count comes out the same.
/// </summary>
/// <param name="context">EF context the reads run on.</param>
public sealed class Wizard4SnapshotGuard : IWizard4SnapshotGuard
{
    private readonly DataBaseContext _context;

    public Wizard4SnapshotGuard(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteInSnapshotAsync<T>(Func<Task<T>> readPhase, CancellationToken ct)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var result = await readPhase();

        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<Wizard4PlanFingerprint> ComputeFingerprintAsync(
        IReadOnlyList<Guid> agentIds, DateOnly from, DateOnly until, CancellationToken ct)
    {
        var ids = agentIds.ToList();

        var works = _context.Work
            .AsNoTracking()
            .Where(w => w.AnalyseToken == null
                && ids.Contains(w.ClientId)
                && w.CurrentDate >= from
                && w.CurrentDate <= until);

        var breaks = _context.Break
            .AsNoTracking()
            .Where(b => b.AnalyseToken == null
                && ids.Contains(b.ClientId)
                && b.CurrentDate >= from
                && b.CurrentDate <= until);

        var workCount = await works.CountAsync(ct);
        var breakCount = await breaks.CountAsync(ct);
        var maxWork = await works.MaxAsync(w => (DateTime?)(w.UpdateTime ?? w.CreateTime), ct);
        var maxBreak = await breaks.MaxAsync(b => (DateTime?)(b.UpdateTime ?? b.CreateTime), ct);

        var maxTimestamp = maxWork is null
            ? maxBreak
            : maxBreak is null
                ? maxWork
                : maxWork > maxBreak ? maxWork : maxBreak;

        return new Wizard4PlanFingerprint(workCount, breakCount, maxTimestamp);
    }
}
