// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default INextPeriodAutoCommitService. Registered as a singleton, like AutoWizardJobRunner whose
/// chain it watches: the detector's tick scope is disposed long before the multi-minute wizard chain
/// finishes, so the watcher polls the singleton job registry, reads the terminal result from the
/// DB-backed JobTerminalStateCache and only then opens a fresh DI scope for the scoped services.
/// The automatic accept runs the SAME AcceptAnalyseScenarioCommand pipeline a human accept uses —
/// including its conflict validation and its Block-mode compliance gate, never overridden — and is
/// additionally preceded by a stricter zero-tolerance gate: one new compliance issue of any severity
/// keeps the scenario a draft. Every outcome is written to the condition ledger and dispatched to the
/// planners for auditability.
/// </summary>
/// <param name="jobRunner">Singleton runner, polled to detect chain completion.</param>
/// <param name="terminalStateCache">DB-backed terminal outcome of the chain (final scenario id/token).</param>
/// <param name="scopeFactory">Creates the fresh scope the scoped compliance/mediator/ledger services need.</param>
/// <param name="logger">Structured log per watched job.</param>

using System.Text.Json;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class NextPeriodAutoCommitService : INextPeriodAutoCommitService
{
    private const int PollIntervalSeconds = 15;
    private const int WatchTimeoutMinutes = 20;
    private const int TerminalStateAttempts = 3;
    private const int TerminalStateRetryDelaySeconds = 2;

    private readonly IAutoWizardJobRunner _jobRunner;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _terminalStateCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NextPeriodAutoCommitService> _logger;

    public NextPeriodAutoCommitService(
        IAutoWizardJobRunner jobRunner,
        JobTerminalStateCache<AutoWizardJobResultDto> terminalStateCache,
        IServiceScopeFactory scopeFactory,
        ILogger<NextPeriodAutoCommitService> logger)
    {
        _jobRunner = jobRunner;
        _terminalStateCache = terminalStateCache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void QueueAutoCommit(Guid jobId, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd)
    {
        _ = Task.Run(() => WatchAndCommitAsync(jobId, groupId, groupName, periodStart, periodEnd));
    }

    internal async Task WatchAndCommitAsync(
        Guid jobId, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd)
    {
        try
        {
            await WatchAndCommitCoreAsync(jobId, groupId, groupName, periodStart, periodEnd);
        }
        catch (Exception ex)
        {
            // The scenario itself is durable whatever happens here; a failed watcher only means the
            // draft waits for a human, which is the safe direction.
            _logger.LogError(ex,
                "NextPeriodAutoCommit: watching job {JobId} for group {GroupName} failed; the scenario stays a draft",
                jobId, groupName);
        }
    }

    private async Task WatchAndCommitCoreAsync(
        Guid jobId, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd)
    {
        if (!await WaitForChainCompletionAsync(jobId))
        {
            _logger.LogWarning(
                "NextPeriodAutoCommit: job {JobId} for group {GroupName} did not finish within {Timeout} minutes; the scenario stays a draft",
                jobId, groupName, WatchTimeoutMinutes);

            return;
        }

        var terminal = await ReadTerminalStateAsync(jobId);
        if (terminal is not
            {
                Status: WizardJobStatusValues.Completed,
                Result: { FinalScenarioId: Guid scenarioId, FinalScenarioToken: Guid scenarioToken }
            })
        {
            _logger.LogInformation(
                "NextPeriodAutoCommit: job {JobId} for group {GroupName} ended without a committable result (status {Status}); nothing to accept",
                jobId, groupName, terminal?.Status ?? WizardJobStatusValues.Unknown);

            return;
        }

        await CommitCompletedChainAsync(scenarioId, scenarioToken, groupId, groupName, periodStart, periodEnd);
    }

    internal async Task CommitCompletedChainAsync(
        Guid scenarioId, Guid scenarioToken, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd)
    {
        using var scope = _scopeFactory.CreateScope();
        var complianceService = scope.ServiceProvider.GetRequiredService<IScenarioComplianceService>();
        var report = await complianceService.EvaluateAsync(
            periodStart, periodEnd, groupId, scenarioToken, CancellationToken.None);

        if (report.NewIssues.Count > 0)
        {
            _logger.LogInformation(
                "NextPeriodAutoCommit: scenario {ScenarioId} for group {GroupName} introduces {Issues} new compliance issue(s) ({Blocking} blocking); accept withheld, scenario stays a draft",
                scenarioId, groupName, report.NewIssues.Count, report.BlockingIssues.Count);

            await PublishAsync(
                scope,
                new NextPeriodAutoCommitBlockedTriggerEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, report.NewIssues.Count));

            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        try
        {
            var accepted = await mediator.Send(new AcceptAnalyseScenarioCommand(scenarioId), CancellationToken.None);
            if (!accepted)
            {
                _logger.LogWarning(
                    "NextPeriodAutoCommit: accepting scenario {ScenarioId} for group {GroupName} was refused; the scenario stays a draft",
                    scenarioId, groupName);

                await PublishAsync(
                    scope,
                    new NextPeriodAutoCommitBlockedTriggerEvent(
                        groupId, groupName, periodStart, periodEnd, scenarioId, report.NewIssues.Count));

                return;
            }
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(
                "NextPeriodAutoCommit: accepting scenario {ScenarioId} for group {GroupName} was blocked by the accept gate ({Reason}); the scenario stays a draft",
                scenarioId, groupName, ex.Message);

            await PublishAsync(
                scope,
                new NextPeriodAutoCommitBlockedTriggerEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, report.NewIssues.Count));

            return;
        }

        _logger.LogInformation(
            "NextPeriodAutoCommit: scenario {ScenarioId} for group {GroupName} accepted into the real schedule (zero new compliance issues)",
            scenarioId, groupName);

        await PublishAsync(
            scope,
            new NextPeriodPlanCommittedTriggerEvent(groupId, groupName, periodStart, periodEnd, scenarioId));
    }

    private async Task<bool> WaitForChainCompletionAsync(Guid jobId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (_jobRunner.IsRunning(jobId))
        {
            if (stopwatch.Elapsed > TimeSpan.FromMinutes(WatchTimeoutMinutes))
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds));
        }

        return true;
    }

    /// <summary>
    /// The registry slot releases just before the terminal row is guaranteed readable, so a miss is
    /// retried a few times instead of being treated as a failed chain.
    /// </summary>
    private async Task<JobTerminalState<AutoWizardJobResultDto>?> ReadTerminalStateAsync(Guid jobId)
    {
        for (var attempt = 1; attempt <= TerminalStateAttempts; attempt++)
        {
            var state = await _terminalStateCache.TryGetAsync(jobId);
            if (state.Found)
            {
                return state;
            }

            if (attempt < TerminalStateAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(TerminalStateRetryDelaySeconds));
            }
        }

        return null;
    }

    /// <summary>
    /// Mirrors the trigger tick's ledger handling for an event raised outside the tick: upsert the
    /// condition row (auditable payload incl. the committed/blocked outcome), dispatch to the
    /// planners, then move a fresh row on to Reported.
    /// </summary>
    private static async Task PublishAsync(IServiceScope scope, IAgentTriggerEvent triggerEvent)
    {
        var ledgerService = scope.ServiceProvider.GetRequiredService<IAgentConditionLedgerService>();
        var triggerService = scope.ServiceProvider.GetRequiredService<IAgentTriggerService>();

        var (condition, _) = await ledgerService.UpsertDetectedAsync(
            triggerEvent.Kind,
            AgentConditionLedgerPolicy.FingerprintFor(triggerEvent),
            triggerEvent.EntityId,
            AgentConditionLedgerPolicy.LedgerGroupIdFor(triggerEvent),
            triggerEvent.Severity,
            JsonSerializer.Serialize(triggerEvent.Payload),
            CancellationToken.None);

        await triggerService.OnEventAsync(triggerEvent, CancellationToken.None);

        if (condition.Status == AgentConditionStatus.Detected)
        {
            await ledgerService.TryTransitionAsync(
                condition.Id,
                AgentConditionStatus.Detected,
                AgentConditionStatus.Reported,
                cancellationToken: CancellationToken.None);
        }
    }
}
