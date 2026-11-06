// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules.PlanningRules;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Logging;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Rules;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Orchestrator that runs Wizard 1 (Planner), Harmonizer (Wizard 2) and Holistic Harmonizer
/// (Wizard 3) sequentially in the backend. Drives each underlying JobRunner, waits for its
/// per-job registry slot to release, materialises the cached result via the matching apply
/// service, and chains the resulting analyse-scenario token into the next stage. Emits a
/// single SignalR event (OnCompleted or OnFailed) at the end of the chain — intermediate
/// stage progress is intentionally not forwarded. The third stage is optional: when its
/// prerequisite is missing (no model configured, or a model known to be text-only) it is skipped
/// and the chain completes with the Harmonizer scenario, flagged as not harmonized.
/// The chain in the requested keyword mode is the run's result and runs under its own budget
/// (AutoWizardTimeBudget.StrictChainMinutes). When the start asks for it, a soft-keyword retry
/// (AutoWizardSoftKeywordRetry) follows under a budget of its own; a better soft scenario that breaks no restricting wish
/// is promoted to the final result (the strict scenario is removed), any other kept one travels as SoftAlternative
/// next to the final scenario and never replaces it. The whole run is bounded by AutoWizardTimeBudget.TotalMinutes.
/// </summary>
/// <param name="scopeFactory">DI scope factory for resolving scoped apply services per stage.</param>
/// <param name="hubNotifier">Sends SignalR OnCompleted/OnFailed events to the job's client group.</param>
/// <param name="registry">Singleton registry that tracks the orchestrator's own cancellation token.</param>
/// <param name="presenceTracker">Records the run under its own presence id for every viewer of its group, from before
/// the start returns until the chain has finished (after its intermediate scenarios are deleted).</param>
/// <param name="wizardRunner">Background runner for the Wizard 1 (Planner) stage.</param>
/// <param name="wizardRegistry">Wizard 1 registry, polled to detect stage completion.</param>
/// <param name="wizardResultCache">Wizard 1 result cache, queried to detect stage success.</param>
/// <param name="harmonizerRunner">Background runner for the Harmonizer (Wizard 2) stage.</param>
/// <param name="harmonizerRegistry">Wizard 2 registry, polled to detect stage completion.</param>
/// <param name="harmonizerResultCache">Wizard 2 result cache, queried to detect stage success.</param>
/// <param name="holisticRunner">Background runner for the Holistic Harmonizer (Wizard 3) stage.</param>
/// <param name="holisticRegistry">Wizard 3 registry, polled to detect stage completion.</param>
/// <param name="holisticStateCache">Wizard 3 terminal states, read to name the real reason a stage failed.</param>
/// <param name="logger">Structured logger for the orchestration trace.</param>
public sealed class AutoWizardJobRunner : IAutoWizardJobRunner
{
    private const int ClientJoinDelayMs = 500;
    private static readonly TimeSpan StrictChainBudget = TimeSpan.FromMinutes(AutoWizardTimeBudget.StrictChainMinutes);
    private static readonly TimeSpan TotalTimeBudget = TimeSpan.FromMinutes(AutoWizardTimeBudget.TotalMinutes);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAutoWizardHubNotifier _hubNotifier;
    private readonly AutoWizardJobRegistry _registry;
    private readonly IAutofillRunPresenceTracker _presenceTracker;
    private readonly AutofillStartGuard _startGuard;
    private readonly AutoWizardStageChain _stageChain;
    private readonly AutoWizardScenarioMaintenance _maintenance;
    private readonly AutoWizardSoftKeywordRetry _softKeywordRetry;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _stateCache;
    private readonly ILogger<AutoWizardJobRunner> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AutoWizardJobRunner(
        IServiceScopeFactory scopeFactory,
        IAutoWizardHubNotifier hubNotifier,
        AutoWizardJobRegistry registry,
        IAutofillRunPresenceTracker presenceTracker,
        AutofillStartGuard startGuard,
        IWizardJobRunner wizardRunner,
        WizardJobRegistry wizardRegistry,
        WizardResultCache wizardResultCache,
        IHarmonizerJobRunner harmonizerRunner,
        HarmonizerJobRegistry harmonizerRegistry,
        HarmonizerResultCache harmonizerResultCache,
        IHolisticHarmonizerJobRunner holisticRunner,
        HolisticHarmonizerJobRegistry holisticRegistry,
        JobTerminalStateCache<HolisticHarmonizerRunResponse> holisticStateCache,
        JobTerminalStateCache<AutoWizardJobResultDto> stateCache,
        IHostApplicationLifetime lifetime,
        ILogger<AutoWizardJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubNotifier = hubNotifier;
        _registry = registry;
        _presenceTracker = presenceTracker;
        _startGuard = startGuard;
        _stageChain = new AutoWizardStageChain(
            scopeFactory,
            wizardRunner,
            wizardRegistry,
            wizardResultCache,
            harmonizerRunner,
            harmonizerRegistry,
            harmonizerResultCache,
            holisticRunner,
            holisticRegistry,
            holisticStateCache,
            logger);
        _maintenance = new AutoWizardScenarioMaintenance(scopeFactory, logger);
        _softKeywordRetry = new AutoWizardSoftKeywordRetry(scopeFactory, _stageChain, _maintenance, logger);
        _stateCache = stateCache;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<Guid> StartAsync(StartAutoWizardRequest request, CancellationToken chainCt)
    {
        var scenarioGroupId = await ResolveStartScopeAsync(request, chainCt);

        _startGuard.EnsureWithinLimits(
            AutofillFamily.AutoWizard, request.AgentIds.Count, request.ShiftIds?.Count ?? 0,
            request.PeriodFrom, request.PeriodUntil);

        var jobId = Guid.NewGuid();
        _startGuard.AcquireRunLock(
            AutofillFamily.AutoWizard, request.PeriodFrom, request.PeriodUntil,
            request.AnalyseToken, request.AgentIds, jobId);

        var cts = _registry.Register(jobId, _lifetime.ApplicationStopping, chainCt);
        cts.CancelAfter(TotalTimeBudget);

        var presence = await _presenceTracker.BeginAsync(scenarioGroupId, request.PeriodFrom, request.PeriodUntil);

        _ = Task.Run(() => RunOrchestrationAsync(jobId, presence.RunId, request, cts.Token));

        return jobId;
    }

    /// <summary>
    /// Refuses a start the caller may not see: a hidden source scenario like a missing one, a hidden target group like a
    /// missing one and - for a restricted caller - a run without a group, whose stage scenarios would be group-less and
    /// therefore refused by the next stage's own scenario check part-way through the chain. Returns the group the run's
    /// scenarios will belong to (ScenarioGroupInheritance, the rule the apply uses).
    /// </summary>
    private async Task<Guid?> ResolveStartScopeAsync(StartAutoWizardRequest request, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IScenarioTokenAccessGuard>().EnsureVisibleAsync(request.AnalyseToken, ct);
        await scope.ServiceProvider.GetRequiredService<IGroupVisibilityGuard>().EnsureScenarioScopeWritableAsync(request.GroupId, ct);

        var sourceScenario = request.AnalyseToken is Guid sourceToken
            ? await scope.ServiceProvider.GetRequiredService<IAnalyseScenarioRepository>().GetByTokenAsync(sourceToken, ct)
            : null;
        return ScenarioGroupInheritance.Resolve(request.GroupId, sourceScenario);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunOrchestrationAsync(Guid jobId, Guid presenceRunId, StartAutoWizardRequest request, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var produced = new List<AutoWizardStageScenario>();

        try
        {
            _logger.LogInformation(
                "AutoWizard job {JobId} starting (period {From} - {Until}, {AgentCount} agents, sourceToken {SourceToken}, keyword mode {KeywordMode}, soft retry {SoftRetry})",
                jobId, request.PeriodFrom, request.PeriodUntil, request.AgentIds.Count, request.AnalyseToken,
                request.KeywordMode, request.SoftKeywordRetry);

            await Task.Delay(ClientJoinDelayMs, ct);

            var requested = await RunRequestedChainAsync(jobId, request, produced, ct);
            var softRetry = await _softKeywordRetry.RunAsync(jobId, request, requested.Chain.FinalScenario, requested.Summary, ct);
            var (result, softRetryOutcome) = await AdoptSoftRetryAsync(jobId, request, requested, softRetry, ct);
            var finalScenario = result.Chain.FinalScenario;

            stopwatch.Stop();

            var dto = new AutoWizardJobResultDto(
                JobId: jobId,
                FinalScenarioId: finalScenario.ScenarioId,
                FinalScenarioToken: finalScenario.Token,
                FinalScenarioName: finalScenario.Name,
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                QualificationGaps: result.QualificationGaps,
                ComplianceViolations: result.Chain.WizardOutcome.ComplianceViolations,
                ComplianceSkippedPlacements: result.Chain.WizardOutcome.SkippedPlacements,
                HarmonizationSkipped: result.Chain.HarmonizationSkippedReason is not null,
                HarmonizationSkippedReason: result.Chain.HarmonizationSkippedReason,
                PlanningRuleWarnings: result.PlanningRuleWarnings,
                PlanningRuleRemaining: result.PlanningRuleRemaining,
                Summary: result.Summary,
                KeywordMode: request.KeywordMode,
                SoftAlternative: softRetry.Alternative,
                SoftKeywordRetryOutcome: softRetryOutcome);

            _logger.LogInformation(
                "AutoWizard job {JobId} completed in {ElapsedMs}ms (final scenario {ScenarioId}/{ScenarioName}, soft-keyword retry {SoftRetryOutcome})",
                jobId, stopwatch.ElapsedMilliseconds, finalScenario.ScenarioId, finalScenario.Name, softRetryOutcome);

            // CancellationToken.None: the chain is already finished. The total time budget may fire while
            // the outcome is being recorded, and a cancelled store would drop it and report the finished
            // chain as cancelled instead.
            await _stateCache.StoreCompletedAsync(jobId, dto, CancellationToken.None);
            if (result.Chain.HarmonizationSkippedReason is { } skippedReason)
            {
                await _maintenance.AnnotateAsync(
                    jobId, finalScenario.ScenarioId, AutoWizardStageOutcomePlanner.BuildHarmonizationSkippedNote(skippedReason));
            }

            await _hubNotifier.NotifyCompletedAsync(jobId, dto);

            // Only the final scenario is a result; the intermediates would otherwise pile up in the
            // scenario list with no way for the operator to tell which one is the real one. The soft-keyword
            // retry has already cleaned up its own scenarios.
            await _maintenance.DeleteAsync(
                jobId,
                AutoWizardSoftKeywordRetryPlanner.StrictScenariosToDelete(
                    produced, softRetryOutcome == AutoWizardSoftKeywordRetryOutcomes.Promoted));
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("AutoWizard job {JobId} cancelled after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await ReportFailureAsync(jobId, request, produced, "AutoWizard run was cancelled or timed out.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AutoWizard job {JobId} failed after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await ReportFailureAsync(jobId, request, produced, ex.Message, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _startGuard.ReleaseRunLock(jobId);
            _registry.Remove(jobId);
            await _presenceTracker.EndAsync(presenceRunId);
        }
    }

    /// <summary>
    /// The chain in the requested keyword mode and the reports on its final scenario, under the strict chain's own
    /// budget: the soft-keyword retry that may follow has a budget of its own, so it can never shorten this run. A run
    /// requested in Soft mode marks its final scenario Soft before anything is reported.
    /// </summary>
    private async Task<AutoWizardRequestedRun> RunRequestedChainAsync(
        Guid jobId, StartAutoWizardRequest request, List<AutoWizardStageScenario> produced, CancellationToken ct)
    {
        using var requestedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestedCts.CancelAfter(StrictChainBudget);
        var requestedCt = requestedCts.Token;

        var chain = await _stageChain.RunAsync(jobId, request, produced, requestedCt);
        if (request.KeywordMode == KeywordMode.Soft)
        {
            await _maintenance.MarkKeywordModeAsync(chain.FinalScenario.ScenarioId, KeywordMode.Soft, note: null);
        }

        return await BuildRunReportsAsync(jobId, request, chain, requestedCt);
    }

    /// <summary>
    /// The run's result after the soft-keyword retry and the retry outcome it reports: the promoted soft chain with its
    /// own reports, otherwise the requested run. When the reports of a promoted chain fail, or the run is cancelled or
    /// reaches its total budget while they are built, the promotion ends like any failed retry (a cancel during the retry
    /// completes with the strict result as well): the promoted scenario is removed and the requested run
    /// stays the result with outcome Failed, so the strict scenario is not deleted with the intermediates.
    /// </summary>
    private async Task<(AutoWizardRequestedRun Run, string SoftRetryOutcome)> AdoptSoftRetryAsync(
        Guid jobId,
        StartAutoWizardRequest request,
        AutoWizardRequestedRun requested,
        AutoWizardSoftKeywordRetryResult softRetry,
        CancellationToken ct)
    {
        if (softRetry.PromotedChain is not { } promotedChain)
        {
            return (requested, softRetry.Outcome);
        }

        try
        {
            return (await BuildRunReportsAsync(jobId, request, promotedChain, ct), softRetry.Outcome);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AutoWizard job {JobId} - the reports of the promoted soft-keyword scenario {ScenarioId} failed or the run reached its budget; the promotion is dropped and the strict scenario {StrictScenarioId} stays the result",
                jobId, promotedChain.FinalScenario.ScenarioId, requested.Chain.FinalScenario.ScenarioId);
            await _maintenance.DeleteAsync(jobId, [promotedChain.FinalScenario.ScenarioId]);
            return (requested, AutoWizardSoftKeywordRetryOutcomes.Failed);
        }
    }

    /// <summary>
    /// The reports on a chain's final scenario: qualification gaps, skipped planning rules, planning-rule findings and the
    /// live summary. Built for the requested chain, and again for a promoted soft-keyword chain that replaces it.
    /// </summary>
    private async Task<AutoWizardRequestedRun> BuildRunReportsAsync(
        Guid jobId, StartAutoWizardRequest request, AutoWizardChainOutcome chain, CancellationToken ct)
    {
        var finalToken = chain.FinalScenario.Token;
        var qualificationGaps = await BuildQualificationGapsAsync(request, finalToken, ct);
        var skippedRuleWarnings = await LoadSkippedPlanningRuleWarningsAsync(request, finalToken, ct);
        var planningRuleRemaining = await CountPlanningRuleHardFindingsAsync(jobId, request, finalToken, ct);
        var summary = await _maintenance.BuildSummaryAsync(jobId, finalToken, ct);

        return new AutoWizardRequestedRun(chain, qualificationGaps, skippedRuleWarnings, planningRuleRemaining, summary);
    }

    /// <summary>
    /// A partial result of a run requested in Soft mode is a soft plan as well; best-effort, the failure is reported
    /// either way.
    /// </summary>
    private async Task MarkPartialKeywordModeAsync(Guid jobId, StartAutoWizardRequest request, Guid partialScenarioId)
    {
        if (request.KeywordMode != KeywordMode.Soft)
        {
            return;
        }

        try
        {
            await _maintenance.MarkKeywordModeAsync(partialScenarioId, KeywordMode.Soft, note: null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} could not mark the partial scenario {ScenarioId} as Soft", jobId, partialScenarioId);
        }
    }

    /// <summary>
    /// Reports the failure with the stage it happened in and keeps the last scenario produced as a partial
    /// result; everything before it is removed so the operator is left with one candidate, not a trail.
    /// </summary>
    private async Task ReportFailureAsync(
        Guid jobId,
        StartAutoWizardRequest request,
        IReadOnlyList<AutoWizardStageScenario> produced,
        string reason,
        long elapsedMs)
    {
        var failure = AutoWizardStageOutcomePlanner.BuildFailure(jobId, produced, AutoWizardStageChain.StageNames.InOrder, reason);
        var statusReason = AutoWizardStageOutcomePlanner.BuildStatusReason(failure);

        // CancellationToken.None: this runs from the cancellation catch as well, where the orchestrator
        // token has already fired - passing it would abort the store and lose the failure reason.
        // The partial result travels along so a status poll can name the scenario that was kept.
        await _stateCache.StoreFailedAsync(
            jobId,
            statusReason,
            AutoWizardStageOutcomePlanner.BuildPartialResult(failure, elapsedMs) is { } partial
                ? partial with { KeywordMode = request.KeywordMode }
                : null,
            CancellationToken.None);
        if (failure.PartialScenarioId is { } partialScenarioId)
        {
            await MarkPartialKeywordModeAsync(jobId, request, partialScenarioId);
            await _maintenance.AnnotateAsync(jobId, partialScenarioId, statusReason);
        }

        await _hubNotifier.NotifyFailedAsync(failure);

        await _maintenance.DeleteAsync(jobId, AutoWizardStageOutcomePlanner.ScenariosToDeleteOnFailure(produced));
    }

    /// <summary>
    /// Stages 2 and 3 skip an invalid approved hard planning constraint instead of failing (Report mode) and plan
    /// with the valid rules; the chain result carries one warning per skipped constraint, with the same
    /// translated key the period check uses, so the user learns that a binding rule was not honoured.
    /// </summary>
    private async Task<IReadOnlyList<ScheduleValidationNotificationDto>> LoadSkippedPlanningRuleWarningsAsync(
        StartAutoWizardRequest request, Guid? finalScenarioToken, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<IPlanningRuleSetLoader>();
        var ruleSet = await loader.LoadRuleSetAsync(
            request.AgentIds,
            request.PeriodFrom,
            request.PeriodUntil,
            finalScenarioToken,
            PlanningConstraintDefaults.MaxDayDistanceLimit + 1,
            PlanningRuleSources.PlanningConstraints,
            InvalidHardRuleHandling.Report,
            ct);
        return PlanningRuleNotificationMapper.ToSkippedRuleWarnings(ruleSet.InvalidHardRuleIds, request.PeriodFrom);
    }

    /// <summary>
    /// Hard planning-rule findings of the source plan and of the final scenario, measured with the evaluator the
    /// Wizard 2/3 guard uses. Wizard 1 does not honour planning rules yet, so the chain may add violations; the
    /// counts let the UI say so honestly. A failure here only drops the summary, never the finished chain.
    /// </summary>
    private async Task<PlanningRuleRemainingDto?> CountPlanningRuleHardFindingsAsync(
        Guid jobId, StartAutoWizardRequest request, Guid? finalScenarioToken, CancellationToken ct)
    {
        if (finalScenarioToken is null)
        {
            return null;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextBuilder = scope.ServiceProvider.GetRequiredService<IHarmonizerContextBuilder>();
            var before = await CountHardFindingsAsync(contextBuilder, request, request.AnalyseToken, ct);
            var after = await CountHardFindingsAsync(contextBuilder, request, finalScenarioToken, ct);
            return before is int hardBefore && after is int hardAfter
                ? new PlanningRuleRemainingDto(hardBefore, hardAfter)
                : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AutoWizard {JobId} - counting the remaining planning-rule violations failed", jobId);
            return null;
        }
    }

    private static async Task<int?> CountHardFindingsAsync(
        IHarmonizerContextBuilder contextBuilder, StartAutoWizardRequest request, Guid? analyseToken, CancellationToken ct)
    {
        var input = await contextBuilder.BuildContextAsync(
            new HarmonizerContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: analyseToken,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter,
                GroupId: request.GroupId),
            ct);
        return BitmapRuleRuntime.TryCreate(input)?.Evaluate(BitmapBuilder.Build(input)).HardCount;
    }

    private async Task<IReadOnlyList<QualificationGapDetail>> BuildQualificationGapsAsync(
        StartAutoWizardRequest request, Guid? finalScenarioToken, CancellationToken ct)
    {
        if (finalScenarioToken is null)
        {
            return [];
        }

        using var scope = _scopeFactory.CreateScope();
        var contextBuilder = scope.ServiceProvider.GetRequiredService<IHarmonizerContextBuilder>();
        var matrixBuilder = scope.ServiceProvider.GetRequiredService<IEligibilityMatrixBuilder>();

        var finalContext = await contextBuilder.BuildContextAsync(
            new HarmonizerContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: finalScenarioToken.Value,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter,
                LoadPlanningRules: false,
                GroupId: request.GroupId),
            ct);

        var nameById = finalContext.Agents.ToDictionary(a => a.Id, a => a.DisplayName);
        var assignments = finalContext.Assignments
            .Where(a => a.ShiftRefId != Guid.Empty)
            .Select(a => (a.AgentId, nameById.GetValueOrDefault(a.AgentId, a.AgentId), a.ShiftRefId, a.Date))
            .ToList();

        var slots = assignments
            .Select(a => new EligibilitySlot(a.ShiftRefId, a.Date))
            .Distinct()
            .ToList();
        // NOTE: no pre-existing-assignment baseline is threaded through here (would need the real,
        // pre-AutoWizard schedule state) — this report can still promote an untouched incumbent's
        // expired-mandatory gap to Error when QUALIFICATION_EXPIRED_MANDATORY_BLOCKS is on. Tracked as a
        // known remaining gap; the other report/veto call sites are baseline-protected.
        var matrix = await matrixBuilder.BuildAsync(request.AgentIds, slots, ct: ct);

        return QualificationGapReportBuilder.BuildAssignedUnqualified(matrix, assignments);
    }
}
