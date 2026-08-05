// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.Scoring;
using Klacks.ScheduleOptimizer.TokenEvolution.Auction.Agent;
using Klacks.ScheduleOptimizer.TokenEvolution.Auction.Conductor;
using Klacks.ScheduleOptimizer.TokenEvolution.Auction.Controller;
using Klacks.ScheduleOptimizer.TokenEvolution.Fitness;
using Klacks.ScheduleOptimizer.TokenEvolution;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Executes wizard GA jobs on the thread-pool and streams progress via SignalR.
/// Each job runs in its own DI scope so scoped repositories/DbContext are properly disposed.
/// </summary>
public sealed class WizardJobRunner : IWizardJobRunner
{
    private const int ClientJoinDelayMs = 500;
    private static readonly TimeSpan WizardTimeBudget = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan HardCancelGrace = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MinLoopBudget = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<WizardJobHub, IWizardJobClient> _hubContext;
    private readonly WizardJobRegistry _registry;
    private readonly WizardResultCache _resultCache;
    private readonly JobTerminalStateCache<WizardJobResultDto> _stateCache;
    private readonly ILogger<WizardJobRunner> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly WizardOptions _options;

    public WizardJobRunner(
        IServiceScopeFactory scopeFactory,
        IHubContext<WizardJobHub, IWizardJobClient> hubContext,
        WizardJobRegistry registry,
        WizardResultCache resultCache,
        JobTerminalStateCache<WizardJobResultDto> stateCache,
        IHostApplicationLifetime lifetime,
        IOptions<WizardOptions> options,
        ILogger<WizardJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _registry = registry;
        _resultCache = resultCache;
        _stateCache = stateCache;
        _lifetime = lifetime;
        _options = options.Value;
        _logger = logger;
    }

    public Task<Guid> StartAsync(WizardContextRequest request, CancellationToken chainCt)
    {
        var jobId = Guid.NewGuid();
        var cts = _registry.Register(jobId, _lifetime.ApplicationStopping, chainCt);

        // The GA loop honours the soft budget itself (config.MaxRuntime) and returns its
        // best-so-far solution; the hard cancel only fires when the job hangs outside the loop.
        cts.CancelAfter(WizardTimeBudget + HardCancelGrace);

        _ = Task.Run(() => RunJobAsync(jobId, request, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunJobAsync(Guid jobId, WizardContextRequest request, CancellationToken ct)
    {
        var group = _hubContext.Clients.Group(SignalRConstants.WizardGroups.WizardJob(jobId));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Wizard job {JobId} starting (period {From} - {Until}, {AgentCount} agents, {ShiftCount} shifts, budget {BudgetSec}s)",
                jobId, request.PeriodFrom, request.PeriodUntil, request.AgentIds.Count, request.ShiftIds?.Count ?? 0, WizardTimeBudget.TotalSeconds);

            await Task.Delay(ClientJoinDelayMs, ct);

            using var scope = _scopeFactory.CreateScope();
            var builder = scope.ServiceProvider.GetRequiredService<IWizardContextBuilder>();
            var wizardContext = await builder.BuildContextAsync(request, ct);
            _logger.LogInformation("Wizard job {JobId} context built", jobId);

            var firstAgentForCapDump = wizardContext.Agents.FirstOrDefault();
            _logger.LogInformation(
                "Wizard job {JobId} effective caps: SchedulingMinPauseHours={MinRest}h, SchedulingMaxDailyHours={MaxDaily}h, SchedulingMaxConsecutiveDays={MaxDays}; firstAgent.MinRestHours={A1}h, firstAgent.MaxDailyHours={A2}h, firstAgent.MaxConsecutiveDays={A3}",
                jobId,
                wizardContext.SchedulingMinPauseHours,
                wizardContext.SchedulingMaxDailyHours,
                wizardContext.SchedulingMaxConsecutiveDays,
                firstAgentForCapDump?.MinRestHours,
                firstAgentForCapDump?.MaxDailyHours,
                firstAgentForCapDump?.MaxConsecutiveDays);

            var baseline = new TokenEvolutionConfig
            {
                RandomSeed = Random.Shared.Next(),
                EvaluationParallelism = _options.EvaluationParallelism,
            };
            var config = request.TrainingOverrides?.Apply(baseline) ?? baseline;
            _logger.LogInformation("Wizard job {JobId} using RandomSeed {Seed}", jobId, config.RandomSeed);

            var remainingBudget = WizardTimeBudget - stopwatch.Elapsed;
            if (remainingBudget < MinLoopBudget)
            {
                remainingBudget = MinLoopBudget;
            }

            config = config with { MaxRuntime = remainingBudget };

            await group.OnProgress(new WizardJobProgressDto(
                JobId: jobId,
                Generation: 0,
                MaxGenerations: config.MaxGenerations,
                BestHardViolations: 0,
                BestStage1Completion: 0d,
                BestStage2Score: 0d,
                EarlyStopping: false));

            var loop = TokenEvolutionLoop.Create();

            var lastLoggedGeneration = 0;
            var lastLoggedAtMs = stopwatch.ElapsedMilliseconds;
            var progress = new Progress<TokenEvolutionProgress>(p =>
            {
                // Per-generation tracing so a slow GA shows where time goes.
                // Throttled to every 10th generation to keep the log readable.
                if (p.Generation - lastLoggedGeneration >= 10 || p.EarlyStopping)
                {
                    var dtMs = stopwatch.ElapsedMilliseconds - lastLoggedAtMs;
                    _logger.LogInformation(
                        "Wizard job {JobId} gen={Generation}/{MaxGen} viol={Viol} stage1={Stage1:F1}% +{DeltaMs}ms",
                        jobId, p.Generation, p.MaxGenerations, p.BestHardViolations, p.BestStage1Completion * 100, dtMs);
                    lastLoggedGeneration = p.Generation;
                    lastLoggedAtMs = stopwatch.ElapsedMilliseconds;
                }
                var sendTask = group.OnProgress(new WizardJobProgressDto(
                    JobId: jobId,
                    Generation: p.Generation,
                    MaxGenerations: p.MaxGenerations,
                    BestHardViolations: p.BestHardViolations,
                    BestStage1Completion: p.BestStage1Completion,
                    BestStage2Score: p.BestStage2Score,
                    EarlyStopping: p.EarlyStopping));
                sendTask.ContinueWith(
                    t => _logger.LogWarning(t.Exception, "Failed to broadcast wizard progress for job {JobId} gen {Generation}", jobId, p.Generation),
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            });

            _logger.LogInformation(
                "Wizard job {JobId} entering GA loop (population={Pop}, maxGen={MaxGen}, agents={Agents}, demandSlots={DemandSlots})",
                jobId, config.PopulationSize, config.MaxGenerations,
                wizardContext.Agents.Count, wizardContext.Shifts.Count);
            var loopStartMs = stopwatch.ElapsedMilliseconds;
            void Trace(string msg) => _logger.LogInformation("Wizard job {JobId} [GA] {Message}", jobId, msg);
            var best = loop.Run(wizardContext, config, progress, ct, Trace);
            var timedOut = stopwatch.Elapsed >= WizardTimeBudget;
            _logger.LogInformation(
                "Wizard job {JobId} GA loop finished in {LoopMs}ms (tokens={TokenCount}, hardViol={Viol}, stage1={Stage1:F1}%, timedOut={TimedOut})",
                jobId, stopwatch.ElapsedMilliseconds - loopStartMs, best.Tokens.Count, best.FitnessStage0, best.FitnessStage1 * 100, timedOut);

            var (awards, escalations) = BuildAuctionTelemetry(wizardContext, config.RandomSeed, _logger);

            // Capture the winning individual's full score decomposition once at run-end (no per-generation
            // cost) and stash it alongside the cached scenario so Apply can persist it into the WizardRunCapture.
            // Best-effort: a scoring failure must never sink the wizard job.
            var subScoreJson = string.Empty;
            var stage0Violations = best.FitnessStage0;
            try
            {
                var evaluator = TokenFitnessEvaluator.Create(wizardContext, config);
                var detailed = evaluator.EvaluateDetailed(best, wizardContext);
                subScoreJson = EngineScoreSerializer.SerializeTokenEvolution(detailed, config, wizardContext);
                stage0Violations = detailed.Stage0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wizard job {JobId} score capture failed; storing empty SubScoreJson", jobId);
            }

            _resultCache.Store(jobId, best, request.AnalyseToken, escalations, subScoreJson, stage0Violations);

            // Wizard 1 never lifts a pre-existing unlocked Work into the genome (ExistingWorkBlockers is
            // veto-only, see IWizardHardConstraintBuilder) — there is no incumbent to protect here.
            var eligibilityBuilder = scope.ServiceProvider.GetRequiredService<IEligibilityMatrixBuilder>();
            var eligibilityMatrix = await eligibilityBuilder.BuildAsync(
                request.AgentIds, EligibilityMatrixBuilder.SlotsFromShifts(wizardContext.Shifts), ct: ct);

            // Two report angles: slots no eligible agent could fill (stayed empty, Error), and agents
            // the wizard DID assign despite a warning-level gap (too-low level / expired / optional).
            // CoreAgent carries no display name, so AgentId is passed and the frontend resolves it.
            var unfillableGaps = QualificationGapReportBuilder.BuildUnfillableSlots(
                eligibilityMatrix, wizardContext.Shifts, wizardContext.Agents, best.Tokens);
            var assignedGaps = QualificationGapReportBuilder.BuildAssignedUnqualified(
                eligibilityMatrix,
                best.Tokens
                    .Where(t => t.ShiftRefId != Guid.Empty)
                    .Select(t => (t.AgentId, string.Empty, t.ShiftRefId, t.Date)));
            var qualificationGaps = unfillableGaps.Concat(assignedGaps).ToList();

            var resultDto = new WizardJobResultDto(
                JobId: jobId,
                FinalHardViolations: best.FitnessStage0,
                FinalStage1Completion: best.FitnessStage1,
                TokenCount: best.Tokens.Count,
                AvailableShiftSlots: wizardContext.Shifts.Count,
                Tokens: MapTokens(best.Tokens),
                Awards: awards,
                Escalations: escalations,
                QualificationGaps: qualificationGaps,
                TimedOut: timedOut,
                SubScoreJson: subScoreJson,
                Stage0Violations: stage0Violations);

            _stateCache.StoreCompleted(jobId, resultDto);
            try
            {
                await group.OnCompleted(resultDto);
            }
            catch (Exception ex)
            {
                // The result is already in the terminal-state cache; a failed broadcast must not turn a
                // finished run into a reported failure. The client recovers via the status endpoint.
                _logger.LogWarning(ex, "Wizard job {JobId} completed but the broadcast failed", jobId);
            }
        }
        catch (OperationCanceledException)
        {
            // The GA loop itself ends gracefully at the soft budget, so this path is only
            // reached when the job hung outside the loop (context build, telemetry, store)
            // long enough for the hard cancel to fire — or when the user cancelled.
            if (stopwatch.Elapsed >= WizardTimeBudget + HardCancelGrace)
            {
                var msg = $"Wizard job exceeded the {(WizardTimeBudget + HardCancelGrace).TotalSeconds:F0}s hard time limit; reduce the agent or shift selection or shorten the period.";
                _logger.LogWarning("Wizard job {JobId} timed out after {Elapsed}: {Message}", jobId, stopwatch.Elapsed, msg);
                _stateCache.StoreFailed(jobId, msg);
                try { await group.OnFailed(msg); } catch { /* notification best-effort */ }
            }
            else
            {
                _logger.LogInformation("Wizard job {JobId} cancelled by user after {Elapsed}", jobId, stopwatch.Elapsed);
                _stateCache.StoreCancelled(jobId);
                try { await group.OnCancelled(); } catch { /* notification best-effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wizard job {JobId} failed", jobId);
            _stateCache.StoreFailed(jobId, ex.Message);
            try
            {
                await group.OnFailed(ex.Message);
            }
            catch
            {
                // Swallow notification errors during failure.
            }
        }
        finally
        {
            _registry.Remove(jobId);
        }
    }

    internal static IReadOnlyList<WizardTokenDto> MapTokens(IEnumerable<CoreToken> tokens) =>
        tokens
            .Where(t => !t.IsLocked)
            .Select(t => new WizardTokenDto(
                AgentId: t.AgentId,
                ShiftId: t.ShiftRefId.ToString(),
                Date: t.Date.ToString("yyyy-MM-dd"),
                StartTime: t.StartAt.ToString("HH:mm"),
                EndTime: t.EndAt.ToString("HH:mm"),
                Hours: t.TotalHours))
            .ToList();

    /// <summary>
    /// Run the auction once outside the GA loop to capture awards + escalations for UI display.
    /// The GA may have mutated the actual best scenario, but the auction telemetry shows what the
    /// rule-based seed produced — useful for explainability ("why did Coline get this slot?").
    /// </summary>
    internal static (IReadOnlyList<WizardAuctionAwardDto> Awards, IReadOnlyList<WizardEscalationDto> Escalations)
        BuildAuctionTelemetry(CoreWizardContext context, int seed, ILogger logger)
    {
        try
        {
            var auctioneer = new SlotAuctioneer(
                new FuzzyBiddingAgent(),
                new Stage0HardConstraintChecker(),
                new Stage1SoftConstraintChecker());
            var outcome = auctioneer.Run(context, new Random(seed));

            var awards = outcome.Results
                .Where(r => r.WinnerAgentId is not null)
                .Select(r => MapAward(r, outcome.Scenario.Tokens))
                .ToList();

            var escalations = outcome.Escalation.Entries
                .Select(e => new WizardEscalationDto(e.AgentId, e.Date.ToString("yyyy-MM-dd"), e.RuleName, e.Hint))
                .ToList();

            return (awards, escalations);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Auction telemetry build failed for seed {Seed}; returning empty awards/escalations", seed);
            return (Array.Empty<WizardAuctionAwardDto>(), Array.Empty<WizardEscalationDto>());
        }
    }

    private static WizardAuctionAwardDto MapAward(
        Klacks.ScheduleOptimizer.TokenEvolution.Auction.Conductor.AuctionResult result,
        IReadOnlyList<CoreToken> tokens)
    {
        var winningBid = result.Bids.FirstOrDefault(b => b.AgentId == result.WinnerAgentId);
        var winningScore = winningBid?.Score ?? 0.0;
        var firedRules = winningBid?.FiredRules ?? Array.Empty<string>();

        var slotParts = result.SlotId.Split('/', 2);
        var date = slotParts.Length == 2 ? slotParts[0] : "";
        var shiftId = slotParts.Length == 2 ? slotParts[1] : result.SlotId;

        return new WizardAuctionAwardDto(
            AgentId: result.WinnerAgentId ?? "",
            Date: date,
            ShiftId: shiftId,
            Round: result.Round,
            WinningScore: winningScore,
            FiredRules: firedRules);
    }
}
