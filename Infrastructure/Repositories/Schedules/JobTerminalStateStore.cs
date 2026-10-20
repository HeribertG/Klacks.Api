// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core implementation of IJobTerminalStateStore over the job_terminal_states table.
/// </summary>
/// <param name="context">Scoped database context of the scope the cache opened for this operation</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class JobTerminalStateStore : IJobTerminalStateStore
{
    // Postgres SQLSTATE for a unique violation, locale-independent unlike the message text.
    private const string UniqueViolationSqlState = "23505";

    private readonly DataBaseContext _context;

    public JobTerminalStateStore(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<JobTerminalStateRow?> FindAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobTerminalStates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);
    }

    public async Task EvictExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await _context.JobTerminalStates
            .Where(r => r.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        _context.JobTerminalStates.RemoveRange(expired);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another writer removed the same expired rows first. Housekeeping, not the caller's
            // concern -- and it must not fail the store operation this eviction merely precedes.
            _context.ChangeTracker.Clear();
        }
    }

    public async Task<bool> TryAddAsync(JobTerminalStateRow row, CancellationToken cancellationToken = default)
    {
        await _context.JobTerminalStates.AddAsync(row, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateJob(exception))
        {
            _context.Entry(row).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsDuplicateJob(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;
}
