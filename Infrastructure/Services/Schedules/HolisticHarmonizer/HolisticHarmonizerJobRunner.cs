// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Executes Holistic Harmonizer jobs on the thread-pool and streams progress via SignalR.
/// Each job runs in its own DI scope so scoped services (settings reader, context builder)
/// are properly disposed. The shared <c>HarmonizerResultCache</c> is populated by
/// <c>HolisticHarmonizerRunService</c> under the same JobId so the existing apply pipeline
/// works unchanged.
/// </summary>
/// <param name="scopeFactory">Factory used to spawn an isolated DI scope per job.</param>
/// <param name="hubContext">SignalR hub context used to broadcast events to the job group.</param>
/// <param name="registry">Singleton registry that links JobId to a cancellation token source.</param>
/// <param name="logger">Diagnostic logger for job lifecycle events and broadcast failures.</param>
public sealed class HolisticHarmonizerJobRunner : IHolisticHarmonizerJobRunner
{
    private const int ClientJoinDelayMs = 500;
    private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(120);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<HolisticHarmonizerJobHub, IHolisticHarmonizerJobClient> _hubContext;
    private readonly HolisticHarmonizerJobRegistry _registry;
    private readonly ILogger<HolisticHarmonizerJobRunner> _logger;

    public HolisticHarmonizerJobRunner(
        IServiceScopeFactory scopeFactory,
        IHubContext<HolisticHarmonizerJobHub, IHolisticHarmonizerJobClient> hubContext,
        HolisticHarmonizerJobRegistry registry,
        ILogger<HolisticHarmonizerJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _registry = registry;
        _logger = logger;
    }

    public Task<Guid> StartAsync(HolisticHarmonizerRunInput input, CancellationToken externalCt)
    {
        ArgumentNullException.ThrowIfNull(input);

        var jobId = Guid.NewGuid();
        var cts = _registry.Register(jobId, externalCt);
        cts.CancelAfter(TimeBudget);

        _ = Task.Run(() => RunJobAsync(jobId, input, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunJobAsync(Guid jobId, HolisticHarmonizerRunInput input, CancellationToken ct)
    {
        var group = _hubContext.Clients.Group(SignalRConstants.HolisticHarmonizerGroups.HolisticHarmonizerJob(jobId));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Holistic Harmonizer job {JobId} starting (period {From} - {Until}, {AgentCount} agents, budget {BudgetSec}s)",
                jobId, input.PeriodFrom, input.PeriodUntil, input.AgentIds.Count, TimeBudget.TotalSeconds);

            await Task.Delay(ClientJoinDelayMs, ct);

            using var scope = _scopeFactory.CreateScope();
            var runService = scope.ServiceProvider.GetRequiredService<HolisticHarmonizerRunService>();

            var progress = new Progress<HolisticHarmonizerProgress>(p => BroadcastProgress(group, jobId, p));

            var outcome = await runService.RunAsync(input, jobId, progress, ct);

            if (!outcome.IsSuccess || outcome.Result is null || outcome.JobId is null)
            {
                var message = outcome.FailureMessage ?? "Holistic Harmonizer run failed.";
                _logger.LogWarning("Holistic Harmonizer job {JobId} failed: {Message}", jobId, message);
                try { await group.OnFailed(message); } catch { /* notification best-effort */ }
                return;
            }

            var response = HolisticHarmonizerResponseMapper.ToResponse(outcome.JobId.Value, outcome.Result);
            await group.OnCompleted(response);

            _logger.LogInformation(
                "Holistic Harmonizer job {JobId} finished in {Ms}ms (fitness {Before:F3} -> {After:F3})",
                jobId, stopwatch.ElapsedMilliseconds, outcome.Result.FitnessBefore, outcome.Result.FitnessAfter);
        }
        catch (OperationCanceledException)
        {
            if (stopwatch.Elapsed >= TimeBudget)
            {
                var msg = $"Holistic Harmonizer exceeded the {TimeBudget.TotalSeconds:F0}s time budget.";
                _logger.LogWarning("Holistic Harmonizer job {JobId} timed out: {Message}", jobId, msg);
                try { await group.OnFailed(msg); } catch { /* notification best-effort */ }
            }
            else
            {
                _logger.LogInformation("Holistic Harmonizer job {JobId} cancelled by user", jobId);
                try { await group.OnCancelled(); } catch { /* notification best-effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holistic Harmonizer job {JobId} failed", jobId);
            try { await group.OnFailed(ex.Message); } catch { /* notification best-effort */ }
        }
        finally
        {
            _registry.Remove(jobId);
        }
    }

    private void BroadcastProgress(IHolisticHarmonizerJobClient group, Guid jobId, HolisticHarmonizerProgress progress)
    {
        var dto = new HolisticHarmonizerJobProgressDto(
            JobId: jobId,
            IterationIndex: progress.IterationIndex,
            MaxIterations: progress.MaxIterations,
            BestFitness: progress.BestFitness,
            AcceptedBatchCount: progress.AcceptedBatchCount,
            RejectedBatchCount: progress.RejectedBatchCount,
            ElapsedMs: progress.ElapsedMs);

        var sendTask = group.OnProgress(dto);
        sendTask.ContinueWith(
            t => _logger.LogWarning(t.Exception, "Failed to broadcast holistic harmonizer progress for job {JobId} iter {Iter}", jobId, progress.IterationIndex),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }
}
