// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Runs the three stages of one AutoWizard chain - Wizard 1 (Planner), Harmonizer (Wizard 2) and Holistic Harmonizer
/// (Wizard 3) - in the keyword mode of the request: drives each stage runner, waits for its registry slot to release,
/// materialises the cached result as a scenario and chains its token into the next stage. Every scenario produced is
/// appended to the caller's list at once, so a failure part-way leaves the caller knowing what to keep or delete. The
/// AutoWizardJobRunner runs it once for the requested mode and, for the soft-keyword retry, a second time in Soft mode.
/// The third stage is optional: when it fails, the chain ends with the Harmonizer scenario and names the reason.
/// </summary>
/// <param name="scopeFactory">DI scope factory for the scoped apply services.</param>
/// <param name="wizardRunner">Background runner for the Wizard 1 (Planner) stage.</param>
/// <param name="wizardRegistry">Wizard 1 registry, polled to detect stage completion.</param>
/// <param name="wizardResultCache">Wizard 1 result cache, queried to detect stage success.</param>
/// <param name="harmonizerRunner">Background runner for the Harmonizer (Wizard 2) stage.</param>
/// <param name="harmonizerRegistry">Wizard 2 registry, polled to detect stage completion.</param>
/// <param name="harmonizerResultCache">Wizard 2 result cache, queried to detect stage success.</param>
/// <param name="holisticRunner">Background runner for the Holistic Harmonizer (Wizard 3) stage.</param>
/// <param name="holisticRegistry">Wizard 3 registry, polled to detect stage completion.</param>
/// <param name="holisticStateCache">Wizard 3 terminal states, read to name the real reason a stage failed.</param>
/// <param name="logger">Structured logger of the orchestrator.</param>
internal sealed class AutoWizardStageChain
{
    private const int PollIntervalMs = 250;
    private static readonly TimeSpan StageTimeout = TimeSpan.FromMinutes(AutoWizardTimeBudget.StageMinutes);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWizardJobRunner _wizardRunner;
    private readonly WizardJobRegistry _wizardRegistry;
    private readonly WizardResultCache _wizardResultCache;
    private readonly IHarmonizerJobRunner _harmonizerRunner;
    private readonly HarmonizerJobRegistry _harmonizerRegistry;
    private readonly HarmonizerResultCache _harmonizerResultCache;
    private readonly IHolisticHarmonizerJobRunner _holisticRunner;
    private readonly HolisticHarmonizerJobRegistry _holisticRegistry;
    private readonly JobTerminalStateCache<HolisticHarmonizerRunResponse> _holisticStateCache;
    private readonly ILogger _logger;

    public AutoWizardStageChain(
        IServiceScopeFactory scopeFactory,
        IWizardJobRunner wizardRunner,
        WizardJobRegistry wizardRegistry,
        WizardResultCache wizardResultCache,
        IHarmonizerJobRunner harmonizerRunner,
        HarmonizerJobRegistry harmonizerRegistry,
        HarmonizerResultCache harmonizerResultCache,
        IHolisticHarmonizerJobRunner holisticRunner,
        HolisticHarmonizerJobRegistry holisticRegistry,
        JobTerminalStateCache<HolisticHarmonizerRunResponse> holisticStateCache,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _wizardRunner = wizardRunner;
        _wizardRegistry = wizardRegistry;
        _wizardResultCache = wizardResultCache;
        _harmonizerRunner = harmonizerRunner;
        _harmonizerRegistry = harmonizerRegistry;
        _harmonizerResultCache = harmonizerResultCache;
        _holisticRunner = holisticRunner;
        _holisticRegistry = holisticRegistry;
        _holisticStateCache = holisticStateCache;
        _logger = logger;
    }

    /// <summary>
    /// Stage names in chain order; used to name the stage a failure stopped in.
    /// </summary>
    public static class StageNames
    {
        public const string Wizard = "Wizard";
        public const string Harmonizer = "Harmonizer";
        public const string HolisticHarmonizer = "HolisticHarmonizer";

        public static readonly string[] InOrder = [Wizard, Harmonizer, HolisticHarmonizer];
    }

    /// <summary>
    /// Runs all stages in the request's keyword mode and appends every scenario they produce to <paramref name="produced"/>.
    /// </summary>
    /// <param name="orchestratorJobId">The orchestrator job, for the log.</param>
    /// <param name="request">The start request; its KeywordMode is the mode of every stage.</param>
    /// <param name="produced">Receives the stage scenarios in chain order; the last one is the chain's result.</param>
    /// <param name="ct">Cancellation of the chain (user cancel, shutdown, budget).</param>
    public async Task<AutoWizardChainOutcome> RunAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        List<AutoWizardStageScenario> produced,
        CancellationToken ct)
    {
        var (wizardScenario, wizardOutcome) = await RunWizardStageAsync(orchestratorJobId, request, ct);
        produced.Add(wizardScenario);

        var harmonizerScenario = await RunHarmonizerStageAsync(orchestratorJobId, request, wizardScenario.Token, ct);
        produced.Add(harmonizerScenario);

        var holisticStage = await RunHolisticStageOrFallBackAsync(orchestratorJobId, request, harmonizerScenario.Token, ct);
        if (holisticStage.Scenario is not null)
        {
            produced.Add(holisticStage.Scenario);
        }

        return new AutoWizardChainOutcome(produced[^1], wizardOutcome, holisticStage.SkippedReason);
    }

    private async Task<(AutoWizardStageScenario Scenario, WizardApplyOutcome Outcome)> RunWizardStageAsync(
        Guid orchestratorJobId, StartAutoWizardRequest request, CancellationToken ct)
    {
        _logger.LogInformation("AutoWizard {JobId} ({KeywordMode}) - stage 1 (Wizard) starting", orchestratorJobId, request.KeywordMode);

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
                AgentOrderIsUserDefined: request.AgentOrderIsUserDefined,
                KeywordMode: request.KeywordMode,
                GroupId: request.GroupId),
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
                "AutoWizard {JobId} ({KeywordMode}) - stage 1 (Wizard) compliance partition blocked {BlockedCount} placement(s); they are excluded from scenario {ScenarioId}",
                orchestratorJobId, request.KeywordMode, outcome.SkippedPlacements.Count, scenario.Id);
        }

        _logger.LogInformation(
            "AutoWizard {JobId} ({KeywordMode}) - stage 1 (Wizard) applied as scenario {ScenarioId} (token {ScenarioToken}, {Count} works)",
            orchestratorJobId, request.KeywordMode, scenario.Id, scenario.Token, outcome.CreatedWorkIds.Count);

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
        _logger.LogInformation("AutoWizard {JobId} ({KeywordMode}) - stage 2 (Harmonizer) starting", orchestratorJobId, request.KeywordMode);

        var stageJobId = await _harmonizerRunner.StartAsync(
            new HarmonizerContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: wizardScenarioToken,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter,
                KeywordMode: request.KeywordMode,
                GroupId: request.GroupId,
                AgentOrderIsUserDefined: request.AgentOrderIsUserDefined),
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
            "AutoWizard {JobId} ({KeywordMode}) - stage 2 (Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, request.KeywordMode, scenario.Id, scenario.Token);

        return new AutoWizardStageScenario(StageNames.Harmonizer, scenario.Id, scenario.Token, scenario.Name);
    }

    /// <summary>
    /// Runs the Holistic Harmonizer stage. Stage 3 only polishes the complete stage-2 plan, so a failure of the
    /// stage never fails the chain: it falls back to the stage-2 scenario and reports the reason as a skipped
    /// harmonization (warning in the log, reason in the run result and on the kept scenario). Cancellation still
    /// ends the chain.
    /// </summary>
    private async Task<(AutoWizardStageScenario? Scenario, string? SkippedReason)> RunHolisticStageOrFallBackAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        Guid harmonizerScenarioToken,
        CancellationToken ct)
    {
        try
        {
            return (await RunHolisticStageAsync(orchestratorJobId, request, harmonizerScenarioToken, ct), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var reason = AutoWizardStageOutcomePlanner.HolisticStageFallbackReason(ex.Message);

            _logger.LogWarning(
                ex,
                "AutoWizard {JobId} ({KeywordMode}) - stage 3 (Holistic Harmonizer) failed; falling back to the stage-2 result: {Reason}",
                orchestratorJobId, request.KeywordMode, reason);
            return (null, reason);
        }
    }

    private async Task<AutoWizardStageScenario> RunHolisticStageAsync(
        Guid orchestratorJobId,
        StartAutoWizardRequest request,
        Guid harmonizerScenarioToken,
        CancellationToken ct)
    {
        _logger.LogInformation("AutoWizard {JobId} ({KeywordMode}) - stage 3 (Holistic Harmonizer) starting", orchestratorJobId, request.KeywordMode);

        var stageJobId = await _holisticRunner.StartAsync(
            new HolisticHarmonizerRunInput(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: harmonizerScenarioToken,
                Language: request.Language,
                ContextDaysBefore: request.ContextDaysBefore,
                ContextDaysAfter: request.ContextDaysAfter,
                KeywordMode: request.KeywordMode,
                GroupId: request.GroupId,
                AgentOrderIsUserDefined: request.AgentOrderIsUserDefined),
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
            "AutoWizard {JobId} ({KeywordMode}) - stage 3 (Holistic Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, request.KeywordMode, scenario.Id, scenario.Token);

        return new AutoWizardStageScenario(StageNames.HolisticHarmonizer, scenario.Id, scenario.Token, scenario.Name);
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
