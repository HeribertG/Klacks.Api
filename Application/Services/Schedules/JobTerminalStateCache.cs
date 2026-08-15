// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Short-lived store of terminal job outcomes (completed/cancelled/failed) so clients that missed the
/// SignalR event (e.g. during a reconnect window) can recover the outcome via a status poll. Entries
/// live in the database rather than in process memory, because a poll may reach a different API
/// instance than the one that ran the job; from there an in-process cache answers "unknown", which a
/// client cannot tell apart from an expired entry, and the failure reason is gone. Entries expire
/// after <see cref="TtlMinutes"/> minutes. Resolves its own scope per call so the surrounding
/// registration can stay a singleton that background runners hold for their lifetime.
/// </summary>
/// <typeparam name="TResult">Result DTO stored for completed jobs.</typeparam>
public sealed class JobTerminalStateCache<TResult>
    where TResult : class
{
    // Postgres SQLSTATE for a unique violation, locale-independent unlike the message text.
    private const string UniqueViolationSqlState = "23505";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobTerminalStateCache<TResult>> _logger;

    public JobTerminalStateCache(IServiceScopeFactory scopeFactory, ILogger<JobTerminalStateCache<TResult>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int TtlMinutes { get; init; } = 15;

    public Task StoreCompletedAsync(Guid jobId, TResult result, CancellationToken cancellationToken = default) =>
        StoreAsync(jobId, WizardJobStatusValues.Completed, result, null, cancellationToken);

    public Task StoreCancelledAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        StoreAsync(jobId, WizardJobStatusValues.Cancelled, null, null, cancellationToken);

    public Task StoreFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default) =>
        StoreAsync(jobId, WizardJobStatusValues.Failed, null, reason, cancellationToken);

    public async Task<JobTerminalState<TResult>> TryGetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

            // Reading deliberately does not evict: two concurrent polls would race over the same
            // expired rows, and the loser's delete -- affecting no rows any more -- would surface as a
            // concurrency failure and answer "unknown" for a job whose row is right there. The expiry
            // check below is what makes a stale row invisible; removing it is the writer's job.
            var row = await context.JobTerminalStates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

            if (row == null || row.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return NotFound();
            }

            var result = row.ResultJson == null ? null : JsonSerializer.Deserialize<TResult>(row.ResultJson);
            return new JobTerminalState<TResult>(true, row.Status, result, row.Reason);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Could not read the terminal state of job {JobId}", jobId);
            return NotFound();
        }
    }

    private static JobTerminalState<TResult> NotFound() =>
        new(false, WizardJobStatusValues.Unknown, null, null);

    private async Task StoreAsync(Guid jobId, string status, TResult? result, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await StoreCoreAsync(jobId, status, result, reason, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogStoreFailure(exception, jobId, status);
        }
    }

    private async Task StoreCoreAsync(Guid jobId, string status, TResult? result, string? reason, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        await EvictExpiredAsync(context, cancellationToken);

        await context.JobTerminalStates.AddAsync(
            new JobTerminalStateRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Status = status,
                ResultJson = result == null ? null : JsonSerializer.Serialize(result),
                Reason = reason,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(TtlMinutes)
            },
            cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateJob(exception))
        {
            // First terminal state wins, enforced by the unique index on JobId. Job ids are unique per
            // run, so a second store can only come from a late failure path -- a completed run must
            // never end up reported as failed. Narrowed to the unique violation on purpose: any other
            // write failure is a real problem and belongs in the log, not silently treated as a
            // duplicate that never happened.
            _logger.LogDebug("Job {JobId} already carries a terminal state; keeping the first one", jobId);
        }
    }

    private static bool IsDuplicateJob(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;

    private void LogStoreFailure(Exception exception, Guid jobId, string status)
    {
        // Recording the outcome must never take the job down with it: the caller is a fire-and-forget
        // runner, and an escaping exception would leave the client polling for a state nobody wrote.
        // Losing the record costs a status poll, which is what the situation looked like before this
        // store existed at all.
        _logger.LogError(exception, "Could not record job {JobId} as {Status}", jobId, status);
    }

    private static async Task EvictExpiredAsync(DataBaseContext context, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var expired = await context.JobTerminalStates
            .Where(r => r.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        context.JobTerminalStates.RemoveRange(expired);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another writer removed the same expired rows first. Housekeeping, not the caller's
            // concern -- and it must not fail the store operation this eviction merely precedes.
            context.ChangeTracker.Clear();
        }
    }
}
