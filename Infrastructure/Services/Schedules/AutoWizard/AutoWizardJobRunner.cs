// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Orchestrator that runs Wizard 1 (Planner), Harmonizer (Wizard 2) and Holistic Harmonizer
/// (Wizard 3) sequentially in the backend. Drives each underlying JobRunner, waits for its
/// per-job registry slot to release, materialises the cached result via the matching apply
/// service, and chains the resulting analyse-scenario token into the next stage. Emits a
/// single SignalR event (OnCompleted or OnFailed) at the end of the chain — intermediate
/// stage progress is intentionally not forwarded.
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
    private readonly IWizardJobRunner _wizardRunner;
    private readonly WizardJobRegistry _wizardRegistry;
    private readonly WizardResultCache _wizardResultCache;
    private readonly IHarmonizerJobRunner _harmonizerRunner;
    private readonly HarmonizerJobRegistry _harmonizerRegistry;
    private readonly HarmonizerResultCache _harmonizerResultCache;
    private readonly IHolisticHarmonizerJobRunner _holisticRunner;
    private readonly HolisticHarmonizerJobRegistry _holisticRegistry;
    private readonly ILogger<AutoWizardJobRunner> _logger;

    public AutoWizardJobRunner(
        IServiceScopeFactory scopeFactory,
        IAutoWizardHubNotifier hubNotifier,
        AutoWizardJobRegistry registry,
        IWizardJobRunner wizardRunner,
        WizardJobRegistry wizardRegistry,
        WizardResultCache wizardResultCache,
        IHarmonizerJobRunner harmonizerRunner,
        HarmonizerJobRegistry harmonizerRegistry,
        HarmonizerResultCache harmonizerResultCache,
        IHolisticHarmonizerJobRunner holisticRunner,
        HolisticHarmonizerJobRegistry holisticRegistry,
        ILogger<AutoWizardJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubNotifier = hubNotifier;
        _registry = registry;
        _wizardRunner = wizardRunner;
        _wizardRegistry = wizardRegistry;
        _wizardResultCache = wizardResultCache;
        _harmonizerRunner = harmonizerRunner;
        _harmonizerRegistry = harmonizerRegistry;
        _harmonizerResultCache = harmonizerResultCache;
        _holisticRunner = holisticRunner;
        _holisticRegistry = holisticRegistry;
        _logger = logger;
    }

    public Task<Guid> StartAsync(StartAutoWizardRequest request, CancellationToken externalCt)
    {
        var jobId = Guid.NewGuid();
        var cts = _registry.Register(jobId, externalCt);
        cts.CancelAfter(TotalTimeBudget);

        _ = Task.Run(() => RunOrchestrationAsync(jobId, request, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunOrchestrationAsync(Guid jobId, StartAutoWizardRequest request, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "AutoWizard job {JobId} starting (period {From} - {Until}, {AgentCount} agents, sourceToken {SourceToken})",
                jobId, request.PeriodFrom, request.PeriodUntil, request.AgentIds.Count, request.AnalyseToken);

            await Task.Delay(ClientJoinDelayMs, ct);

            var wizardScenarioToken = await RunWizardStageAsync(jobId, request, ct);
            var harmonizerScenarioToken = await RunHarmonizerStageAsync(jobId, request, wizardScenarioToken, ct);
            var (finalScenarioId, finalScenarioToken, finalScenarioName) =
                await RunHolisticStageAsync(jobId, request, harmonizerScenarioToken, ct);

            stopwatch.Stop();

            var dto = new AutoWizardJobResultDto(
                JobId: jobId,
                FinalScenarioId: finalScenarioId,
                FinalScenarioToken: finalScenarioToken,
                FinalScenarioName: finalScenarioName,
                ElapsedMs: stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "AutoWizard job {JobId} completed in {ElapsedMs}ms (final scenario {ScenarioId}/{ScenarioName})",
                jobId, stopwatch.ElapsedMilliseconds, finalScenarioId, finalScenarioName);

            await _hubNotifier.NotifyCompletedAsync(jobId, dto);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("AutoWizard job {JobId} cancelled after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await _hubNotifier.NotifyFailedAsync(jobId, "AutoWizard run was cancelled or timed out.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AutoWizard job {JobId} failed after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            await _hubNotifier.NotifyFailedAsync(jobId, ex.Message);
        }
        finally
        {
            _registry.Remove(jobId);
        }
    }

    private async Task<Guid> RunWizardStageAsync(Guid orchestratorJobId, StartAutoWizardRequest request, CancellationToken ct)
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
                ContextDaysAfter: request.ContextDaysAfter),
            ct);

        await WaitForStageAsync(_wizardRegistry.IsRunning, stageJobId, "Wizard", ct);

        if (!_wizardResultCache.TryGet(stageJobId, out _, out _, out _))
        {
            throw new InvalidOperationException("Wizard stage did not produce a result.");
        }

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IWizardApplyService>();
        var (scenario, createdIds) = await apply.ApplyAsScenarioAsync(stageJobId, request.GroupId, ct, "Auto-Erstellung Plan");

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 1 (Wizard) applied as scenario {ScenarioId} (token {ScenarioToken}, {Count} works)",
            orchestratorJobId, scenario.Id, scenario.Token, createdIds.Count);

        return scenario.Token;
    }

    private async Task<Guid> RunHarmonizerStageAsync(
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

        if (!_harmonizerResultCache.TryGet(stageJobId, out _, out _, out _))
        {
            throw new InvalidOperationException("Harmonizer stage did not produce a result.");
        }

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IHarmonizerApplyService>();
        var (scenario, _) = await apply.ApplyAsScenarioAsync(stageJobId, request.GroupId, ct, "Auto-Erstellung Harmonizer");

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 2 (Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, scenario.Id, scenario.Token);

        return scenario.Token;
    }

    private async Task<(Guid? Id, Guid? Token, string? Name)> RunHolisticStageAsync(
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

        await WaitForStageAsync(_holisticRegistry.IsRunning, stageJobId, "Holistic Harmonizer", ct);

        using var scope = _scopeFactory.CreateScope();
        var apply = scope.ServiceProvider.GetRequiredService<IHolisticHarmonizerApplyService>();

        AnalyseScenarioResource scenario;
        try
        {
            (scenario, _) = await apply.ApplyAsScenarioAsync(stageJobId, request.GroupId, ct, "Auto-Erstellung");
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException("Holistic Harmonizer stage did not produce a result.", ex);
        }

        _logger.LogInformation(
            "AutoWizard {JobId} - stage 3 (Holistic Harmonizer) applied as scenario {ScenarioId} (token {ScenarioToken})",
            orchestratorJobId, scenario.Id, scenario.Token);

        return (scenario.Id, scenario.Token, scenario.Name);
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
