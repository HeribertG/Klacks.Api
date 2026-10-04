// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Interfaces;
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
/// </summary>
/// <param name="scopeFactory">DI scope factory for resolving scoped apply services per stage.</param>
/// <param name="hubNotifier">Sends SignalR OnCompleted/OnFailed events to the job's client group.</param>
/// <param name="registry">Singleton registry that tracks the orchestrator's own cancellation token.</param>
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
    private const int PollIntervalMs = 250;
    private static readonly TimeSpan StageTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TotalTimeBudget = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAutoWizardHubNotifier _hubNotifier;
    private readonly AutoWizardJobRegistry _registry;
    private readonly AutofillStartGuard _startGuard;
    private readonly IWizardJobRunner _wizardRunner;
    private readonly WizardJobRegistry _wizardRegistry;
    private readonly WizardResultCache _wizardResultCache;
    private readonly IHarmonizerJobRunner _harmonizerRunner;
    private readonly HarmonizerJobRegistry _harmonizerRegistry;
    private readonly HarmonizerResultCache _harmonizerResultCache;
    private readonly IHolisticHarmonizerJobRunner _holisticRunner;
    private readonly HolisticHarmonizerJobRegistry _holisticRegistry;
    private readonly JobTerminalStateCache<HolisticHarmonizerRunResponse> _holisticStateCache;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _stateCache;
    private readonly ILogger<AutoWizardJobRunner> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AutoWizardJobRunner(
        IServiceScopeFactory scopeFactory,
        IAutoWizardHubNotifier hubNotifier,
        AutoWizardJobRegistry registry,
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
        _startGuard = startGuard;
        _wizardRunner = wizardRunner;
        _wizardRegistry = wizardRegistry;
        _wizardResultCache = wizardResultCache;
        _harmonizerRunner = harmonizerRunner;
        _harmonizerRegistry = harmonizerRegistry;
        _harmonizerResultCache = harmonizerResultCache;
        _holisticRunner = holisticRunner;
        _holisticRegistry = holisticRegistry;
        _holisticStateCache = holisticStateCache;
        _stateCache = stateCache;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task<Guid> StartAsync(StartAutoWizardRequest request, CancellationToken chainCt)
    {
        _startGuard.EnsureWithinLimits(
            AutofillFamily.AutoWizard, request.AgentIds.Count, request.ShiftIds?.Count ?? 0,
            request.PeriodFrom, request.PeriodUntil);

        var jobId = Guid.NewGuid();
        _startGuard.AcquireRunLock(
            AutofillFamily.AutoWizard, request.PeriodFrom, request.PeriodUntil,
            request.AnalyseToken, request.AgentIds, jobId);

        var cts = _registry.Register(jobId, _lifetime.ApplicationStopping, chainCt);
        cts.CancelAfter(TotalTimeBudget);

        _ = Task.Run(() => RunOrchestrationAsync(jobId, request, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunOrchestrationAsync(Guid jobId, StartAutoWizardRequest request, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var produced = new List<AutoWizardStageScenario>();

        try
        {
            _logger.LogInformation(
                "AutoWizard job {JobId} starting (period {From} - {Until}, {AgentCount} agents, sourceToken {SourceToken})",
                jobId, request.PeriodFrom, request.PeriodUntil, request.AgentIds.Count, request.AnalyseToken);

            await Task.Delay(ClientJoinDelayMs, ct);

            var (wizardScenario, wizardOutcome) = await RunWizardStageAsync(jobId, request, ct);
            produced.Add(wizardScenario);

            var harmonizerScenario = await RunHarmonizerStageAsync(jobId, request, wizardScenario.Token, ct);
            produced.Add(harmonizerScenario);

            var holisticStage = await RunHolisticStageOrSkipAsync(jobId, request, harmonizerScenario.Token, ct);
            if (holisticStage.Scenario is not null)
            {
                produced.Add(holisticStage.Scenario);
            }

            var finalScenario = produced[^1];

            var qualificationGaps = await BuildQualificationGapsAsync(request, finalScenario.Token, ct);
            var skippedRuleWarnings = await LoadSkippedPlanningRuleWarningsAsync(request, finalScenario.Token, ct);
            var planningRuleRemaining = await CountPlanningRuleHardFindingsAsync(jobId, request, finalScenario.Token, ct);

            stopwatch.Stop();

            var dto = new AutoWizardJobResultDto(
                JobId: jobId,
                FinalScenarioId: finalScenario.ScenarioId,
                FinalScenarioToken: finalScenario.Token,
                FinalScenarioName: finalScenario.Name,
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                QualificationGaps: qualificationGaps,
                ComplianceViolations: wizardOutcome.ComplianceViolations,
                ComplianceSkippedPlacements: wizardOutcome.SkippedPlacements,
                HarmonizationSkipped: holisticStage.SkippedReason is not null,
                HarmonizationSkippedReason: holisticStage.SkippedReason,
                PlanningRuleWarnings: skippedRuleWarnings,
                PlanningRuleRemaining: planningRuleRemaining);

            _logger.LogInformation(
                "AutoWizard job {JobId} completed in {ElapsedMs}ms (final scenario {ScenarioId}/{ScenarioName})",
                jobId, stopwatch.ElapsedMilliseconds, finalScenario.ScenarioId, finalScenario.Name);

            // CancellationToken.None: the chain is already finished. The total time budget may fire while
            // the outcome is being recorded, and a cancelled store would drop it and report the finished
            // chain as cancelled instead.
            await _stateCache.StoreCompletedAsync(jobId, dto, CancellationToken.None);
            if (holisticStage.SkippedReason is not null)
            {
                await AnnotateScenarioAsync(
                    jobId, finalScenario.ScenarioId,
                    AutoWizardStageOutcomePlanner.BuildHarmonizationSkippedNote(holisticStage.SkippedReason));
            }

            await _hubNotifier.NotifyCompletedAsync(jobId, dto);

            // Only the final scenario is a result; the intermediates would otherwise pile up in the
            // scenario list with no way for the operator to tell which one is the real one.
            await DeleteScenariosAsync(
                jobId, AutoWizardStageOutcomePlanner.ScenariosToDeleteOnSuccess(produced), ct);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("AutoWizard job {JobId} cancelled after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await ReportFailureAsync(jobId, produced, "AutoWizard run was cancelled or timed out.", stopwatch.ElapsedMilliseconds, ct);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AutoWizard job {JobId} failed after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await ReportFailureAsync(jobId, produced, ex.Message, stopwatch.ElapsedMilliseconds, ct);
        }
        finally
        {
            _startGuard.ReleaseRunLock(jobId);
            _registry.Remove(jobId);
        }
    }

    /// <summary>
    /// Stage names in chain order; used to name the stage a failure stopped in.
    /// </summary>
    private static class StageNames
    {
        public const string Wizard = "Wizard";
        public const string Harmonizer = "Harmonizer";
        public const string HolisticHarmonizer = "HolisticHarmonizer";

        public static readonly string[] InOrder = [Wizard, Harmonizer, HolisticHarmonizer];
    }

    /// <summary>
    /// Reports the failure with the stage it happened in and keeps the last scenario produced as a partial
    /// result; everything before it is removed so the operator is left with one candidate, not a trail.
    /// </summary>
    private async Task ReportFailureAsync(
        Guid jobId,
        IReadOnlyList<AutoWizardStageScenario> produced,
        string reason,
        long elapsedMs,
        CancellationToken ct)
    {
        var failure = AutoWizardStageOutcomePlanner.BuildFailure(jobId, produced, StageNames.InOrder, reason);
        var statusReason = AutoWizardStageOutcomePlanner.BuildStatusReason(failure);

        // CancellationToken.None: this runs from the cancellation catch as well, where the orchestrator
        // token has already fired - passing it would abort the store and lose the failure reason.
        // The partial result travels along so a status poll can name the scenario that was kept.
        await _stateCache.StoreFailedAsync(
            jobId,
            statusReason,
            AutoWizardStageOutcomePlanner.BuildPartialResult(failure, elapsedMs),
            CancellationToken.None);
        if (failure.PartialScenarioId is { } partialScenarioId)
        {
            await AnnotateScenarioAsync(jobId, partialScenarioId, statusReason);
        }

        await _hubNotifier.NotifyFailedAsync(failure);

        await DeleteScenariosAsync(jobId, AutoWizardStageOutcomePlanner.ScenariosToDeleteOnFailure(produced), ct);
    }

    /// <summary>
    /// Appends the chain outcome to the scenario's description, best-effort. The terminal state expires after
    /// minutes and the chat history carries no job id, so the scenario is the durable place where the assistant
    /// and the operator read that a run failed or skipped its holistic harmonization.
    /// </summary>
    private async Task AnnotateScenarioAsync(Guid jobId, Guid scenarioId, string note)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAnalyseScenarioRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var scenario = await repository.Get(scenarioId);
            if (scenario == null)
            {
                return;
            }

            scenario.Description = string.IsNullOrWhiteSpace(scenario.Description)
                ? note
                : $"{scenario.Description} | {note}";
            await repository.Put(scenario);
            await unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} could not note its outcome on scenario {ScenarioId}", jobId, scenarioId);
        }
    }

    /// <summary>
    /// Removes intermediate scenarios best-effort - a cleanup failure must never turn a finished or already
    /// reported run into something else.
    /// </summary>
    private async Task DeleteScenariosAsync(Guid jobId, IReadOnlyList<Guid> scenarioIds, CancellationToken ct)
    {
        if (scenarioIds.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            foreach (var scenarioId in scenarioIds)
            {
                await mediator.Send(new DeleteAnalyseScenarioCommand(scenarioId), ct);
            }

            _logger.LogInformation(
                "AutoWizard job {JobId} removed {Count} intermediate scenario(s)", jobId, scenarioIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} could not remove its intermediate scenarios", jobId);
        }
    }

    private async Task<(AutoWizardStageScenario Scenario, WizardApplyOutcome Outcome)> RunWizardStageAsync(
        Guid orchestratorJobId, StartAutoWizardRequest request, CancellationToken ct)
    {
        _logger.LogInformation("AutoWizard {JobId} - stage 1 (Wizard) starting", orchestratorJobId);

        var stageJobId = await _wizardRunner.StartAsync(
            new WizardContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                ShiftIds: request.ShiftIds,
                AnalyseToken: request.AnalyseToken,
                TrainingOverrides: null,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter,
                AgentOrderIsUserDefined: request.AgentOrderIsUserDefined),
            ct);

        await WaitForStageAsync(_wizardRegistry.IsRunning, stageJobId, "Wizard", ct);

        if (!_wizardResultCache.TryGet(stageJobId, out _, out _, out _, out _, out _))
        {
            throw new InvalidOperationException("Wizard stage did not produce a result.");
        }

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IWizardApplyService>();
        var (scenario, outcome) = await apply.ApplyAsScenarioAsync(
            stageJobId, request.GroupId, overrideBlock: false, ct, nameKind: ScenarioNameKind.AutoPlan, language: request.Language);

        if (outcome.SkippedPlacements.Count > 0)
        {
            _logger.LogWarning(
                "AutoWizard {JobId} - stage 1 (Wizard) compliance partition blocked {BlockedCount} placement(s); they are excluded from scenario {ScenarioId}",
                orchestratorJobId, outcome.SkippedPlacements.Count, scenario.Id);
        }

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 1 (Wizard) applied as scenario {ScenarioId} (token {ScenarioToken}, {Count} works)",
            orchestratorJobId, scenario.Id, scenario.Token, outcome.CreatedWorkIds.Count);

        return (
            new AutoWizardStageScenario(StageNames.Wizard, scenario.Id, scenario.Token, scenario.Name),
            outcome);
    }

    private async Task<AutoWizardStageScenario> RunHarmonizerStageAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        Guid wizardScenarioToken,
        CancellationToken ct)
    {
        _logger.LogInformation("AutoWizard {JobId} - stage 2 (Harmonizer) starting", orchestratorJobId);

        var stageJobId = await _harmonizerRunner.StartAsync(
            new HarmonizerContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: wizardScenarioToken,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter),
            ct);

        await WaitForStageAsync(_harmonizerRegistry.IsRunning, stageJobId, "Harmonizer", ct);

        if (!_harmonizerResultCache.TryGet(stageJobId, out _, out _, out _, out _, out _, out _))
        {
            throw new InvalidOperationException("Harmonizer stage did not produce a result.");
        }

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IHarmonizerApplyService>();
        // evaluateCompliance: false — the intermediate stage result has no reader for the report;
        // the accept gate protects the real plan when the final scenario is promoted.
        var (scenario, _, _) = await apply.ApplyAsScenarioAsync(
            stageJobId, request.GroupId, ct, nameKind: ScenarioNameKind.AutoHarmonizer, language: request.Language, captureRun: true, evaluateCompliance: false);

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 2 (Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, scenario.Id, scenario.Token);

        return new AutoWizardStageScenario(StageNames.Harmonizer, scenario.Id, scenario.Token, scenario.Name);
    }

    /// <summary>
    /// Runs the Holistic Harmonizer stage when its prerequisite is met and skips it otherwise. Stage 3 only
    /// polishes the complete stage-2 plan, so a failure of the stage never fails the chain: it falls back to
    /// the stage-2 scenario and reports the reason as a skipped harmonization (warning in the log, reason in
    /// the run result and on the kept scenario). Cancellation still ends the chain.
    /// </summary>
    private async Task<(AutoWizardStageScenario? Scenario, string? SkippedReason)> RunHolisticStageOrSkipAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        Guid harmonizerScenarioToken,
        CancellationToken ct)
    {
        var readiness = await CheckHolisticReadinessAsync(ct);
        if (!readiness.IsReady)
        {
            _logger.LogWarning(
                "AutoWizard {JobId} - stage 3 (Holistic Harmonizer) skipped: {Reason}",
                orchestratorJobId, readiness.Reason);
            return (null, readiness.Reason);
        }

        try
        {
            return (await RunHolisticStageAsync(orchestratorJobId, request, harmonizerScenarioToken, ct), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var readinessAfterFailure = await CheckHolisticReadinessAsync(ct);
            var reason = AutoWizardStageOutcomePlanner.HolisticStageFallbackReason(readinessAfterFailure, ex.Message);

            _logger.LogWarning(
                ex,
                "AutoWizard {JobId} - stage 3 (Holistic Harmonizer) failed; falling back to the stage-2 result: {Reason}",
                orchestratorJobId, reason);
            return (null, reason);
        }
    }

    private async Task<HolisticHarmonizerReadiness> CheckHolisticReadinessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var readinessCheck = scope.ServiceProvider.GetRequiredService<IHolisticHarmonizerReadinessCheck>();
        return await readinessCheck.CheckAsync(ct);
    }

    private async Task<AutoWizardStageScenario> RunHolisticStageAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        Guid harmonizerScenarioToken,
        CancellationToken ct)
    {
        _logger.LogInformation("AutoWizard {JobId} - stage 3 (Holistic Harmonizer) starting", orchestratorJobId);

        var stageJobId = await _holisticRunner.StartAsync(
            new HolisticHarmonizerRunInput(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: harmonizerScenarioToken,
                Language: request.Language,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter),
            ct);

        try
        {
            await WaitForStageAsync(_holisticRegistry.IsRunning, stageJobId, "Holistic Harmonizer", ct);
        }
        catch (TimeoutException)
        {
            _holisticRunner.TryCancel(stageJobId);
            throw;
        }

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IHolisticHarmonizerApplyService>();

        AnalyseScenarioResource scenario;
        try
        {
            // evaluateCompliance: false — the AutoWizard completion payload carries the Wizard-1 stage
            // report; the final scenario is protected at the accept gate when the user promotes it.
            (scenario, _, _) = await apply.ApplyAsScenarioAsync(
                stageJobId, request.GroupId, ct, nameKind: ScenarioNameKind.Auto, language: request.Language, captureRun: true, evaluateCompliance: false);
        }
        catch (InvalidOperationException ex)
        {
            var stageState = await _holisticStateCache.TryGetAsync(stageJobId, CancellationToken.None);
            var stageReason = stageState.Found && !string.IsNullOrWhiteSpace(stageState.Reason)
                ? $" {stageState.Reason}"
                : string.Empty;
            throw new InvalidOperationException($"Holistic Harmonizer stage did not produce a result.{stageReason}", ex);
        }

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 3 (Holistic Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, scenario.Id, scenario.Token);

        return new AutoWizardStageScenario(StageNames.HolisticHarmonizer, scenario.Id, scenario.Token, scenario.Name);
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
                ContextDaysAfter: request.ContextDaysAfter),
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
                LoadPlanningRules: false),
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

    private static async Task WaitForStageAsync(
        Func<Guid, bool> isRunning,
        Guid stageJobId,
        string stageName,
        CancellationToken ct)
    {
        using var stageCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stageCts.CancelAfter(StageTimeout);

        try
        {
            while (isRunning(stageJobId))
            {
                await Task.Delay(PollIntervalMs, stageCts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"{stageName} stage did not finish within {StageTimeout.TotalMinutes} minutes.");
        }
    }

}
