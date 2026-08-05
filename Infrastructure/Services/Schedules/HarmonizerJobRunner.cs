// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.Harmonizer.Telemetry;
using Klacks.ScheduleOptimizer.Scoring;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Executes harmonizer jobs on the thread-pool and streams progress via SignalR. Each job
/// runs in its own DI scope so scoped repositories/DbContext are properly disposed.
/// </summary>
public sealed class HarmonizerJobRunner : IHarmonizerJobRunner
{
    private const int ClientJoinDelayMs = 500;
    private const double DefaultEmergencyThreshold = 0.5;

    // Conductor-only: the raw seed plus exactly one conductor pass, no generations.
    private const int ConductorOnlyPopulationSize = 2;
    private const int ConductorOnlyMaxGenerations = 0;
    private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan HardCancelGrace = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MinLoopBudget = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<HarmonizerJobHub, IHarmonizerJobClient> _hubContext;
    private readonly HarmonizerJobRegistry _registry;
    private readonly AutofillStartGuard _startGuard;
    private readonly HarmonizerResultCache _resultCache;
    private readonly JobTerminalStateCache<HarmonizerJobResultDto> _stateCache;
    private readonly ILogger<HarmonizerJobRunner> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly bool _useEvolution;

    public HarmonizerJobRunner(
        IServiceScopeFactory scopeFactory,
        IHubContext<HarmonizerJobHub, IHarmonizerJobClient> hubContext,
        HarmonizerJobRegistry registry,
        AutofillStartGuard startGuard,
        HarmonizerResultCache resultCache,
        JobTerminalStateCache<HarmonizerJobResultDto> stateCache,
        IOptions<HarmonizerOptions> harmonizerOptions,
        IHostApplicationLifetime lifetime,
        ILogger<HarmonizerJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _registry = registry;
        _startGuard = startGuard;
        _resultCache = resultCache;
        _stateCache = stateCache;
        _useEvolution = harmonizerOptions.Value.UseEvolution;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <summary>
    /// Builds the loop configuration for the selected engine mode. Conductor-only means a population of
    /// exactly two - the untouched seed plus one full conductor pass - and no generations, so the loop
    /// returns the better of the two. The GA defaults apply only when evolution is switched on.
    /// </summary>
    /// <param name="useEvolution">True selects the genetic loop, false the single conductor pass.</param>
    /// <param name="remainingBudget">Wall-clock budget left for the loop.</param>
    /// <param name="seed">Random seed; recorded with the run so the result can be replayed.</param>
    internal static HarmonizerEvolutionConfig BuildEvolutionConfig(bool useEvolution, TimeSpan remainingBudget, int seed)
        => useEvolution
            ? new HarmonizerEvolutionConfig(Seed: seed, MaxRuntime: remainingBudget)
            : new HarmonizerEvolutionConfig(
                PopulationSize: ConductorOnlyPopulationSize,
                MaxGenerations: ConductorOnlyMaxGenerations,
                Seed: seed,
                MaxRuntime: remainingBudget);

    public Task<Guid> StartAsync(HarmonizerContextRequest request, CancellationToken chainCt)
    {
        _startGuard.EnsureWithinLimits(
            AutofillFamily.Harmonizer, request.AgentIds.Count, 0, request.PeriodFrom, request.PeriodUntil);

        var jobId = Guid.NewGuid();
        _startGuard.AcquireRunLock(
            AutofillFamily.Harmonizer, request.PeriodFrom, request.PeriodUntil,
            request.AnalyseToken, request.AgentIds, jobId);

        var cts = _registry.Register(jobId, _lifetime.ApplicationStopping, chainCt);

        // The evolution loop honours the soft budget itself (config.MaxRuntime) and returns
        // its best-so-far arrangement; the hard cancel only fires when the job hangs outside it.
        cts.CancelAfter(TimeBudget + HardCancelGrace);

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
                "Harmonizer job {JobId} starting in {Mode} mode (period {From} - {Until}, {AgentCount} agents, budget {BudgetSec}s)",
                jobId, _useEvolution ? "evolution" : "conductor-only", request.PeriodFrom, request.PeriodUntil,
                request.AgentIds.Count, TimeBudget.TotalSeconds);

            await Task.Delay(ClientJoinDelayMs, ct);

            using var scope = _scopeFactory.CreateScope();
            var contextBuilder = scope.ServiceProvider.GetRequiredService<IHarmonizerContextBuilder>();
            var input = await contextBuilder.BuildContextAsync(request, ct);

            // Fingerprint of what this run is about to optimise. Apply compares it again so manual
            // changes made while the job ran are not silently overwritten.
            var snapshotMarker = await scope.ServiceProvider
                .GetRequiredService<IScheduleSnapshotMarkerService>()
                .ComputeAsync(request.PeriodFrom, request.PeriodUntil, request.AgentIds, request.AnalyseToken, ct);

            var sortedBitmap = RowSorter.Sort(BitmapBuilder.Build(input));
            var originalForCache = CloneBitmap(sortedBitmap);

            var scorer = new HarmonyScorer();
            var validator = new DomainAwareReplaceValidator(input.Availability, input.BoundaryAssignments, input.IneligibleAssignments);
            var fitness = new HarmonyFitnessEvaluator(scorer);
            var stochasticMutation = new StochasticBitmapMutation(validator);

            var remainingBudget = TimeBudget - stopwatch.Elapsed;
            if (remainingBudget < MinLoopBudget)
            {
                remainingBudget = MinLoopBudget;
            }

            var seed = request.Seed ?? Random.Shared.Next();
            var config = BuildEvolutionConfig(_useEvolution, remainingBudget, seed);
            _logger.LogInformation("Harmonizer job {JobId} using RandomSeed {Seed}", jobId, seed);

            HarmonizerConductor BuildConductor(int rowCount)
            {
                var emergencyState = new EmergencyUnlockState(rowCount);
                var emergency = new EmergencyUnlockManager(emergencyState, DefaultEmergencyThreshold);
                var mutation = new ReplaceMutation(scorer, validator);
                var blockSwap = new BlockSwapMutation(scorer, validator);
                return new HarmonizerConductor(scorer, mutation, emergency, hints: input.SofteningHints, blockSwapMutation: blockSwap);
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
            var timedOut = stopwatch.Elapsed >= TimeBudget;

            // Belt-and-braces anytime guarantee: the loop keeps the raw seed in its population, so this
            // gate should never fire. If it ever does, evaluator and population have drifted apart and
            // the operator gets the untouched original plan back instead of a worse one.
            var best = result.Best;
            if (best.Fitness < initialFitness.Fitness)
            {
                _logger.LogWarning(
                    "Harmonizer job {JobId} regression gate fired: best {Best:F3} < seed {Seed:F3}; returning original plan",
                    jobId, best.Fitness, initialFitness.Fitness);
                best = HarmonizerEvolutionLoop.CreateUnprocessedIndividual(
                    originalForCache, initialFitness.Fitness, initialFitness.RowScores);
            }

            var rowResults = BuildRowResults(originalForCache, best, scorer);

            // Scan the produced plan for assignments whose agent lacks a required mandatory
            // qualification (pre-existing ones the harmonizer left untouched surface here too).
            var matrixBuilder = scope.ServiceProvider.GetRequiredService<IEligibilityMatrixBuilder>();
            var finalAssignments = QualificationGapReportBuilder.ExtractBitmapAssignments(best.Bitmap);
            var eligibilitySlots = finalAssignments
                .Select(a => new EligibilitySlot(a.ShiftId, a.Date))
                .Distinct()
                .ToList();
            // Pre-Commit-Diff-Prinzip: an unlocked incumbent from the ORIGINAL (pre-harmonizer) plan must
            // not be retroactively promoted to Error by the opt-in expired-mandatory escalation just
            // because the harmonizer left it untouched; a cell the harmonizer newly assigned still gates.
            var incumbentAssignments = ExtractIncumbentAssignments(originalForCache);
            var eligibilityMatrix = await matrixBuilder.BuildAsync(request.AgentIds, eligibilitySlots, incumbentAssignments, ct);
            var qualificationGaps = QualificationGapReportBuilder.BuildAssignedUnqualified(eligibilityMatrix, finalAssignments);

            // Bridge the score snapshot for the (deferred) preference-learner into the cache, mirroring the
            // Wizard-1 WizardResultCache pattern: the apply path reads it back and writes it onto the capture
            // row. Stage-0 proxy is the assigned-unqualified count (the harmonizer's hard-violation surface).
            var subScoreJson = string.Empty;
            var stage0Violations = qualificationGaps.Count;
            try
            {
                subScoreJson = EngineScoreSerializer.SerializeHarmonizer(best.Bitmap, config, best.Fitness);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Harmonizer job {JobId} score capture failed; storing empty SubScoreJson", jobId);
            }

            _resultCache.Store(jobId, originalForCache, best.Bitmap, request.AnalyseToken, subScoreJson, stage0Violations, snapshotMarker);

            var resultDto = new HarmonizerJobResultDto(
                JobId: jobId,
                GlobalFitnessBefore: initialFitness.Fitness,
                GlobalFitnessAfter: best.Fitness,
                GenerationsRun: result.GenerationFitness.Count - 1,
                RowResults: rowResults,
                QualificationGaps: qualificationGaps,
                TimedOut: timedOut);

            _stateCache.StoreCompleted(jobId, resultDto);
            try
            {
                await group.OnCompleted(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Harmonizer job {JobId} completed but the broadcast failed", jobId);
            }

            _logger.LogInformation(
                "Harmonizer job {JobId} finished in {Ms}ms (fitness {Before:F3} -> {After:F3}, timedOut={TimedOut})",
                jobId, stopwatch.ElapsedMilliseconds, initialFitness.Fitness, best.Fitness, timedOut);

            await EmitTelemetryAsync(
                scope, jobId, request, initialFitness.Fitness, best, result.GenerationFitness.Count - 1,
                stopwatch.ElapsedMilliseconds, ct);
        }
        catch (OperationCanceledException)
        {
            // The evolution loop ends gracefully at the soft budget; this path is only
            // reached when the job hung outside the loop or the user cancelled.
            if (stopwatch.Elapsed >= TimeBudget + HardCancelGrace)
            {
                var msg = $"Harmonizer exceeded the {(TimeBudget + HardCancelGrace).TotalSeconds:F0}s hard time limit.";
                _logger.LogWarning("Harmonizer job {JobId} timed out: {Message}", jobId, msg);
                _stateCache.StoreFailed(jobId, msg);
                try { await group.OnFailed(msg); } catch { /* notification best-effort */ }
            }
            else
            {
                _logger.LogInformation("Harmonizer job {JobId} cancelled by user", jobId);
                _stateCache.StoreCancelled(jobId);
                try { await group.OnCancelled(); } catch { /* notification best-effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Harmonizer job {JobId} failed", jobId);
            _stateCache.StoreFailed(jobId, ex.Message);
            try { await group.OnFailed(ex.Message); } catch { /* notification best-effort */ }
        }
        finally
        {
            _startGuard.ReleaseRunLock(jobId);
            _registry.Remove(jobId);
        }
    }

    private static HarmonyBitmap CloneBitmap(HarmonyBitmap source) => BitmapCloner.Clone(source);

    private static IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> ExtractIncumbentAssignments(HarmonyBitmap original)
    {
        var incumbents = new HashSet<(string AgentId, Guid ShiftId, DateOnly Date)>();
        for (var r = 0; r < original.RowCount; r++)
        {
            var agent = original.Rows[r];
            for (var d = 0; d < original.DayCount; d++)
            {
                var cell = original.GetCell(r, d);
                if (cell.ShiftRefId is Guid shiftId && shiftId != Guid.Empty && !cell.IsLocked)
                {
                    incumbents.Add((agent.Id, shiftId, original.Days[d]));
                }
            }
        }
        return incumbents;
    }

    private static IReadOnlyList<HarmonizerRowResultDto> BuildRowResults(
        HarmonyBitmap original,
        Individual best,
        HarmonyScorer scorer)
    {
        // Score the original bitmap (untouched seed) and the best harmonised bitmap with the
        // same scorer. RowSorter ran on both so row indices are aligned. The ConductorTrace
        // only reflects the last conductor pass on the best individual, where most score
        // changes already happened during stochastic mutation — using its before/after would
        // make the per-row diff appear flat even though global fitness improved.
        var trace = best.ConductorTrace;
        var results = new List<HarmonizerRowResultDto>(best.Bitmap.RowCount);
        for (var r = 0; r < best.Bitmap.RowCount; r++)
        {
            var scoreBefore = scorer.Score(original, r).Score;
            var scoreAfter = scorer.Score(best.Bitmap, r).Score;
            var emergencyTriggered = r < trace.RowTraces.Count && trace.RowTraces[r].EmergencyUnlockTriggered;
            results.Add(new HarmonizerRowResultDto(
                AgentId: best.Bitmap.Rows[r].Id,
                ScoreBefore: scoreBefore,
                ScoreAfter: scoreAfter,
                EmergencyUnlockTriggered: emergencyTriggered));
        }
        return results;
    }

    private async Task EmitTelemetryAsync(
        IServiceScope scope,
        Guid jobId,
        HarmonizerContextRequest request,
        double initialFitness,
        Individual best,
        int generationsRun,
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

            var trace = best.ConductorTrace;
            var rowTelemetry = new List<RowTelemetry>(best.Bitmap.RowCount);
            var emergencyUnlocks = 0;
            for (var r = 0; r < best.Bitmap.RowCount; r++)
            {
                var rowTrace = trace.RowTraces[r];
                if (rowTrace.EmergencyUnlockTriggered)
                {
                    emergencyUnlocks++;
                }
                rowTelemetry.Add(new RowTelemetry(
                    AgentId: best.Bitmap.Rows[r].Id,
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
                RowCount: best.Bitmap.RowCount,
                InitialFitness: initialFitness,
                FinalFitness: best.Fitness,
                EmergencyThreshold: DefaultEmergencyThreshold,
                GenerationsRun: generationsRun,
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
