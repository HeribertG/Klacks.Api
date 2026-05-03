// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.Harmonizer.Telemetry;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Executes harmonizer jobs on the thread-pool and streams progress via SignalR. Each job
/// runs in its own DI scope so scoped repositories/DbContext are properly disposed.
/// </summary>
public sealed class HarmonizerJobRunner : IHarmonizerJobRunner
{
    private const int ClientJoinDelayMs = 500;
    private const double DefaultEmergencyThreshold = 0.5;
    private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(120);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<HarmonizerJobHub, IHarmonizerJobClient> _hubContext;
    private readonly HarmonizerJobRegistry _registry;
    private readonly HarmonizerResultCache _resultCache;
    private readonly ILogger<HarmonizerJobRunner> _logger;

    public HarmonizerJobRunner(
        IServiceScopeFactory scopeFactory,
        IHubContext<HarmonizerJobHub, IHarmonizerJobClient> hubContext,
        HarmonizerJobRegistry registry,
        HarmonizerResultCache resultCache,
        ILogger<HarmonizerJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _registry = registry;
        _resultCache = resultCache;
        _logger = logger;
    }

    public Task<Guid> StartAsync(HarmonizerContextRequest request, CancellationToken externalCt)
    {
        var jobId = Guid.NewGuid();
        var cts = _registry.Register(jobId, externalCt);
        cts.CancelAfter(TimeBudget);

        _ = Task.Run(() => RunJobAsync(jobId, request, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunJobAsync(Guid jobId, HarmonizerContextRequest request, CancellationToken ct)
    {
        var group = _hubContext.Clients.Group(SignalRConstants.HarmonizerGroups.HarmonizerJob(jobId));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Harmonizer job {JobId} starting (period {From} - {Until}, {AgentCount} agents, budget {BudgetSec}s)",
                jobId, request.PeriodFrom, request.PeriodUntil, request.AgentIds.Count, TimeBudget.TotalSeconds);

            await Task.Delay(ClientJoinDelayMs, ct);

            using var scope = _scopeFactory.CreateScope();
            var contextBuilder = scope.ServiceProvider.GetRequiredService<IHarmonizerContextBuilder>();
            var input = await contextBuilder.BuildContextAsync(request, ct);

            var sortedBitmap = RowSorter.Sort(BitmapBuilder.Build(input));
            var originalForCache = CloneBitmap(sortedBitmap);

            var scorer = new HarmonyScorer();
            var validator = new DomainAwareReplaceValidator(input.Availability);
            var fitness = new HarmonyFitnessEvaluator(scorer);
            var stochasticMutation = new StochasticBitmapMutation(validator);
            var config = new HarmonizerEvolutionConfig();

            HarmonizerConductor BuildConductor(int rowCount)
            {
                var emergencyState = new EmergencyUnlockState(rowCount);
                var emergency = new EmergencyUnlockManager(emergencyState, DefaultEmergencyThreshold);
                var mutation = new ReplaceMutation(scorer, validator);
                return new HarmonizerConductor(scorer, mutation, emergency, hints: input.SofteningHints);
            }

            var loop = new HarmonizerEvolutionLoop(fitness, stochasticMutation, BuildConductor, config);
            var initialFitness = fitness.Evaluate(sortedBitmap);

            var progress = new Progress<EvolutionGenerationProgress>(p =>
            {
                var sendTask = group.OnProgress(new HarmonizerJobProgressDto(
                    JobId: jobId,
                    Generation: p.Generation,
                    MaxGenerations: p.MaxGenerations,
                    BestFitness: p.BestFitness,
                    EarlyStopping: p.EarlyStopping));
                sendTask.ContinueWith(
                    t => _logger.LogWarning(t.Exception, "Failed to broadcast harmonizer progress for job {JobId} gen {Generation}", jobId, p.Generation),
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            });

            var result = await Task.Run(() => loop.Run(sortedBitmap, progress, ct), ct);

            _resultCache.Store(jobId, originalForCache, result.Best.Bitmap, request.AnalyseToken);

            var rowResults = BuildRowResults(result.Best);
            await group.OnCompleted(new HarmonizerJobResultDto(
                JobId: jobId,
                GlobalFitnessBefore: initialFitness.Fitness,
                GlobalFitnessAfter: result.Best.Fitness,
                GenerationsRun: result.GenerationFitness.Count - 1,
                RowResults: rowResults));

            _logger.LogInformation(
                "Harmonizer job {JobId} finished in {Ms}ms (fitness {Before:F3} -> {After:F3})",
                jobId, stopwatch.ElapsedMilliseconds, initialFitness.Fitness, result.Best.Fitness);

            await EmitTelemetryAsync(scope, jobId, request, initialFitness.Fitness, result, stopwatch.ElapsedMilliseconds, ct);
        }
        catch (OperationCanceledException)
        {
            if (stopwatch.Elapsed >= TimeBudget)
            {
                var msg = $"Harmonizer exceeded the {TimeBudget.TotalSeconds:F0}s time budget.";
                _logger.LogWarning("Harmonizer job {JobId} timed out: {Message}", jobId, msg);
                try { await group.OnFailed(msg); } catch { /* notification best-effort */ }
            }
            else
            {
                _logger.LogInformation("Harmonizer job {JobId} cancelled by user", jobId);
                try { await group.OnCancelled(); } catch { /* notification best-effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Harmonizer job {JobId} failed", jobId);
            try { await group.OnFailed(ex.Message); } catch { /* notification best-effort */ }
        }
        finally
        {
            _registry.Remove(jobId);
        }
    }

    private static HarmonyBitmap CloneBitmap(HarmonyBitmap source) => BitmapCloner.Clone(source);

    private static IReadOnlyList<HarmonizerRowResultDto> BuildRowResults(Individual best)
    {
        var trace = best.ConductorTrace;
        var results = new List<HarmonizerRowResultDto>(best.Bitmap.RowCount);
        for (var r = 0; r < best.Bitmap.RowCount; r++)
        {
            var rowTrace = trace.RowTraces[r];
            results.Add(new HarmonizerRowResultDto(
                AgentId: best.Bitmap.Rows[r].Id,
                ScoreBefore: rowTrace.ScoreBefore,
                ScoreAfter: rowTrace.ScoreAfter,
                EmergencyUnlockTriggered: rowTrace.EmergencyUnlockTriggered));
        }
        return results;
    }

    private async Task EmitTelemetryAsync(
        IServiceScope scope,
        Guid jobId,
        HarmonizerContextRequest request,
        double initialFitness,
        EvolutionResult result,
        long durationMs,
        CancellationToken ct)
    {
        try
        {
            var sink = scope.ServiceProvider.GetService<IHarmonizerTelemetrySink>();
            if (sink is null)
            {
                return;
            }

            var trace = result.Best.ConductorTrace;
            var rowTelemetry = new List<RowTelemetry>(result.Best.Bitmap.RowCount);
            var emergencyUnlocks = 0;
            for (var r = 0; r < result.Best.Bitmap.RowCount; r++)
            {
                var rowTrace = trace.RowTraces[r];
                if (rowTrace.EmergencyUnlockTriggered)
                {
                    emergencyUnlocks++;
                }
                rowTelemetry.Add(new RowTelemetry(
                    AgentId: result.Best.Bitmap.Rows[r].Id,
                    RowIndex: r,
                    InitialScore: rowTrace.ScoreBefore,
                    FinalScore: rowTrace.ScoreAfter,
                    MovesApplied: rowTrace.MovesApplied,
                    EmergencyUnlockTriggered: rowTrace.EmergencyUnlockTriggered));
            }

            var telemetry = new HarmonizerRunTelemetry(
                JobId: jobId,
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                RowCount: result.Best.Bitmap.RowCount,
                InitialFitness: initialFitness,
                FinalFitness: result.Best.Fitness,
                EmergencyThreshold: DefaultEmergencyThreshold,
                GenerationsRun: result.GenerationFitness.Count - 1,
                TotalEmergencyUnlocks: emergencyUnlocks,
                DurationMs: durationMs,
                Rows: rowTelemetry);

            await sink.RecordAsync(telemetry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Harmonizer telemetry sink failed for job {JobId}", jobId);
        }
    }
}
