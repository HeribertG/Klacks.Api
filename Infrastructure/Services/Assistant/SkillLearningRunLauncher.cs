// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Starts learning runs and keeps them from overlapping. Inside this process a single gate does that
/// outright: the six-hourly tick and an administrator's manual trigger cannot run at the same time, and
/// the manual trigger is told so instead of silently queueing behind one. Across instances the guarantee
/// comes from the per-cluster compare-and-swap claim in the loop, not from here - two runs on two
/// machines would claim disjoint clusters, so no cluster is ever learned twice.
/// The manual path starts the run in the background: a run rebuilds the knowledge index several times and
/// takes minutes, which no HTTP request may wait for. The tick runs as Scheduled, the manual start as Manual:
/// in Gate only a manual start measures description proposals.
/// It also keeps the status of the latest run (start, end, success, error, counts, trigger) for the
/// run-status endpoint.
/// </summary>
/// <param name="scopeFactory">Creates the scoped provider a run needs, independent of the caller's scope</param>
/// <param name="logger">Reports runs that failed outright</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class SkillLearningRunLauncher : ISkillLearningRunLauncher
{
    private const string AlreadyRunning = "A learning run is already in progress.";
    private const string CancelledReason = "The learning run was cancelled.";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillLearningRunLauncher> _logger;
    private readonly object _statusLock = new();
    private SkillLearningRunStatus _status = SkillLearningRunStatus.Idle;

    public SkillLearningRunLauncher(
        IServiceScopeFactory scopeFactory,
        ILogger<SkillLearningRunLauncher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<SkillLearningRunTicket> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_gate.Wait(0, cancellationToken))
        {
            return SkillLearningRunTicket.Refused(AlreadyRunning);
        }

        MarkStarted(SkillLearningRunTrigger.Scheduled);
        await ExecuteAsync(SkillLearningRunTrigger.Scheduled, cancellationToken);
        return SkillLearningRunTicket.Accepted();
    }

    public SkillLearningRunTicket StartDetached()
    {
        if (!_gate.Wait(0))
        {
            return SkillLearningRunTicket.Refused(AlreadyRunning);
        }

        MarkStarted(SkillLearningRunTrigger.Manual);
        _ = Task.Run(() => ExecuteAsync(SkillLearningRunTrigger.Manual, CancellationToken.None));
        return SkillLearningRunTicket.Accepted();
    }

    public SkillLearningRunStatus GetStatus()
    {
        lock (_statusLock)
        {
            return _status;
        }
    }

    private void MarkStarted(SkillLearningRunTrigger trigger)
    {
        lock (_statusLock)
        {
            _status = _status with { Running = true, LastStartedUtc = DateTime.UtcNow, LastTrigger = trigger };
        }
    }

    private async Task ExecuteAsync(SkillLearningRunTrigger trigger, CancellationToken cancellationToken)
    {
        SkillLearningRunSummary? summary = null;
        string? error = null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loop = scope.ServiceProvider.GetRequiredService<ISkillLearningLoop>();
            summary = await loop.RunAsync(trigger, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            error = CancelledReason;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Skill learning run failed");
            error = exception.Message;
        }
        finally
        {
            lock (_statusLock)
            {
                _status = _status with
                {
                    Running = false,
                    LastFinishedUtc = DateTime.UtcNow,
                    LastSucceeded = error == null,
                    LastError = error,
                    LastSummary = summary
                };
            }

            _gate.Release();
        }
    }
}
