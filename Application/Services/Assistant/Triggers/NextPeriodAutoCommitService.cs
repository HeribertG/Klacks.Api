// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default INextPeriodAutoCommitService. Registered as a singleton, like AutoWizardJobRunner whose
/// chain it watches: the detector's tick scope is disposed long before the multi-minute wizard chain
/// finishes, so the watcher polls the singleton job registry, reads the terminal result from the
/// DB-backed JobTerminalStateCache and only then opens a fresh DI scope for the scoped services.
/// The automatic accept runs the SAME AcceptAnalyseScenarioCommand pipeline a human accept uses -
/// including its conflict validation and its Block-mode compliance gate, never overridden - and is
/// additionally preceded by a stricter zero-tolerance gate: one new compliance issue of any severity
/// keeps the scenario a draft. A watched chain can take up to
/// NextPeriodScheduling.AutoCommitWatchTimeoutMinutes to finish, so BOTH governance inputs are re-read
/// right before the commit itself and not only by the detector at tick time: the global proactive kill
/// switch, and the effective autonomy level - an admin who lowers their level during the wait has
/// withdrawn consent for this very accept. The accept is not anonymous either: it is sent under the
/// admin whose preference released the run, and the same id is recorded as the ledger row's approving
/// user.
/// EVERY way of not committing raises a NextPeriodAutoCommitBlockedTriggerEvent with its own reason.
/// A silent failure is the one outcome this branch must never produce - the planners would keep
/// believing the period was committed while it is still a draft.
/// The whole watch hangs off IHostApplicationLifetime.ApplicationStopping, so a shutdown ends the wait
/// instead of holding the host, and is reported as information rather than as a failure.
/// </summary>
/// <param name="jobRunner">Singleton runner, polled to detect chain completion.</param>
/// <param name="terminalStateCache">DB-backed terminal outcome of the chain (final scenario id/token).</param>
/// <param name="scopeFactory">Creates the fresh scope the scoped compliance/mediator/ledger/governance services need.</param>
/// <param name="applicationLifetime">Source of the shutdown token every wait and every scoped call is bound to.</param>
/// <param name="timeProvider">Measures the watch window; the wait must not read the wall clock directly.</param>
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
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class NextPeriodAutoCommitService : INextPeriodAutoCommitService
{
    private const int PollIntervalSeconds = 15;
    private const int TerminalStateAttempts = 3;
    private const int TerminalStateRetryDelaySeconds = 2;
    private const int NoNewIssues = 0;
    private const AutonomyLevel CommitMinimumLevel = AutonomyLevel.FullyAutonomous;

    private readonly IAutoWizardJobRunner _jobRunner;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _terminalStateCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NextPeriodAutoCommitService> _logger;

    public NextPeriodAutoCommitService(
        IAutoWizardJobRunner jobRunner,
        JobTerminalStateCache<AutoWizardJobResultDto> terminalStateCache,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime applicationLifetime,
        TimeProvider timeProvider,
        ILogger<NextPeriodAutoCommitService> logger)
    {
        _jobRunner = jobRunner;
        _terminalStateCache = terminalStateCache;
        _scopeFactory = scopeFactory;
        _applicationLifetime = applicationLifetime;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public void QueueAutoCommit(Guid jobId, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd)
    {
        var stoppingToken = _applicationLifetime.ApplicationStopping;

        _ = Task.Run(
            () => WatchAndCommitAsync(jobId, groupId, groupName, periodStart, periodEnd, stoppingToken),
            stoppingToken);
    }

    internal async Task WatchAndCommitAsync(
        Guid jobId,
        Guid groupId,
        string groupName,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            await WatchAndCommitCoreAsync(jobId, groupId, groupName, periodStart, periodEnd, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown, not a failure: the scenario is durable and waits for a human, and the next start
            // of the API finds it again through the detector's interrupted check.
            _logger.LogInformation(
                "NextPeriodAutoCommit: watching job {JobId} for group {GroupName} was stopped by host shutdown; the scenario stays a draft",
                jobId, groupName);
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
        Guid jobId,
        Guid groupId,
        string groupName,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        if (!await WaitForChainCompletionAsync(jobId, cancellationToken))
        {
            _logger.LogWarning(
                "NextPeriodAutoCommit: job {JobId} for group {GroupName} did not finish within {Timeout} minutes; the scenario stays a draft",
                jobId, groupName, NextPeriodScheduling.AutoCommitWatchTimeoutMinutes);

            await PublishBlockedAsync(
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, null, NoNewIssues,
                    NextPeriodAutoCommitBlockReason.Timeout),
                cancellationToken);

            return;
        }

        var terminal = await ReadTerminalStateAsync(jobId, cancellationToken);
        if (terminal is not
            {
                Status: WizardJobStatusValues.Completed,
                Result: { FinalScenarioId: Guid scenarioId, FinalScenarioToken: Guid scenarioToken }
            })
        {
            _logger.LogInformation(
                "NextPeriodAutoCommit: job {JobId} for group {GroupName} ended without a committable result (status {Status}); nothing to accept",
                jobId, groupName, terminal?.Status ?? WizardJobStatusValues.Unknown);

            await PublishBlockedAsync(
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, null, NoNewIssues,
                    NextPeriodAutoCommitBlockReason.NotCommittable),
                cancellationToken);

            return;
        }

        await CommitCompletedChainAsync(
            scenarioId, scenarioToken, groupId, groupName, periodStart, periodEnd, cancellationToken);
    }

    internal async Task CommitCompletedChainAsync(
        Guid scenarioId,
        Guid scenarioToken,
        Guid groupId,
        string groupName,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var governanceResolver = scope.ServiceProvider.GetRequiredService<IProactiveGovernanceResolver>();
        if (await governanceResolver.IsKillSwitchActiveAsync(cancellationToken))
        {
            _logger.LogWarning(
                "NextPeriodAutoCommit: proactive kill switch is active; withholding auto-accept for scenario {ScenarioId}, group {GroupName} - the scenario stays a draft",
                scenarioId, groupName);

            await PublishAsync(
                scope,
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, NoNewIssues,
                    NextPeriodAutoCommitBlockReason.KillSwitch),
                cancellationToken);

            return;
        }

        var complianceService = scope.ServiceProvider.GetRequiredService<IScenarioComplianceService>();
        var report = await complianceService.EvaluateAsync(
            periodStart, periodEnd, groupId, scenarioToken, cancellationToken);

        if (report.NewIssues.Count > 0)
        {
            _logger.LogInformation(
                "NextPeriodAutoCommit: scenario {ScenarioId} for group {GroupName} introduces {Issues} new compliance issue(s) ({Blocking} blocking); accept withheld, scenario stays a draft",
                scenarioId, groupName, report.NewIssues.Count, report.BlockingIssues.Count);

            await PublishAsync(
                scope,
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, report.NewIssues.Count,
                    NextPeriodAutoCommitBlockReason.NewViolations),
                cancellationToken);

            return;
        }

        var autonomyResolver = scope.ServiceProvider.GetRequiredService<INextPeriodAutonomyResolver>();
        var autonomy = await autonomyResolver.ResolveAsync(cancellationToken);
        if (autonomy.EffectiveLevel < CommitMinimumLevel)
        {
            _logger.LogWarning(
                "NextPeriodAutoCommit: effective autonomy level fell to {Level} while job for group {GroupName} was watched; withholding auto-accept for scenario {ScenarioId}",
                autonomy.EffectiveLevel, groupName, scenarioId);

            await PublishAsync(
                scope,
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, NoNewIssues,
                    NextPeriodAutoCommitBlockReason.AutonomyLowered),
                cancellationToken);

            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        try
        {
            var accepted = await mediator.Send(
                new AcceptAnalyseScenarioCommand(scenarioId, false, autonomy.DecidingAdminUserId),
                cancellationToken);

            if (!accepted)
            {
                _logger.LogWarning(
                    "NextPeriodAutoCommit: accepting scenario {ScenarioId} for group {GroupName} was refused; the scenario stays a draft",
                    scenarioId, groupName);

                await PublishAsync(
                    scope,
                    BlockedEvent(
                        groupId, groupName, periodStart, periodEnd, scenarioId, NoNewIssues,
                        NextPeriodAutoCommitBlockReason.Refused),
                    cancellationToken);

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
                BlockedEvent(
                    groupId, groupName, periodStart, periodEnd, scenarioId, NoNewIssues,
                    NextPeriodAutoCommitBlockReason.Conflict),
                cancellationToken);

            return;
        }

        _logger.LogInformation(
            "NextPeriodAutoCommit: scenario {ScenarioId} for group {GroupName} accepted into the real schedule on behalf of admin {AdminUserId} (zero new compliance issues)",
            scenarioId, groupName, autonomy.DecidingAdminUserId);

        await PublishAsync(
            scope,
            new NextPeriodPlanCommittedTriggerEvent(groupId, groupName, periodStart, periodEnd, scenarioId),
            cancellationToken,
            autonomy.DecidingAdminUserId);
    }

    private static NextPeriodAutoCommitBlockedTriggerEvent BlockedEvent(
        Guid groupId,
        string groupName,
        DateOnly periodStart,
        DateOnly periodEnd,
        Guid? scenarioId,
        int newIssueCount,
        NextPeriodAutoCommitBlockReason reason) =>
        new(groupId, groupName, periodStart, periodEnd, scenarioId, newIssueCount, reason);

    private async Task<bool> WaitForChainCompletionAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow()
            .AddMinutes(NextPeriodScheduling.AutoCommitWatchTimeoutMinutes);
        while (_jobRunner.IsRunning(jobId))
        {
            if (_timeProvider.GetUtcNow() >= deadline)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// The registry slot releases just before the terminal row is guaranteed readable, so a miss is
    /// retried a few times instead of being treated as a failed chain.
    /// </summary>
    private async Task<JobTerminalState<AutoWizardJobResultDto>?> ReadTerminalStateAsync(
        Guid jobId, CancellationToken cancellationToken)
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
                await Task.Delay(TimeSpan.FromSeconds(TerminalStateRetryDelaySeconds), cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// The failure paths that run before <see cref="CommitCompletedChainAsync"/> has opened its own scope
    /// still have to be reported, so they get a scope of their own rather than staying silent.
    /// </summary>
    private async Task PublishBlockedAsync(
        NextPeriodAutoCommitBlockedTriggerEvent blockedEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        await PublishAsync(scope, blockedEvent, cancellationToken);
    }

    /// <summary>
    /// Mirrors the trigger tick's ledger handling for an event raised outside the tick: upsert the
    /// condition row (auditable payload incl. the committed/blocked outcome), dispatch to the
    /// planners, then move a fresh row on to Reported.
    /// </summary>
    /// <param name="approvedByUserId">
    /// The admin whose standing autonomy preference released an automatic accept, stamped onto the row of
    /// the committed event. The accept handler's own write-back cannot do it here: that one only fires for
    /// a condition row carrying the scenario as its remediation, and an autofill scenario is produced by
    /// the wizard chain, not prepared for a finding. Null on every blocked outcome - nothing was approved.
    /// </param>
    private static async Task PublishAsync(
        IServiceScope scope,
        IAgentTriggerEvent triggerEvent,
        CancellationToken cancellationToken,
        Guid? approvedByUserId = null)
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
            cancellationToken);

        await triggerService.OnEventAsync(triggerEvent, cancellationToken);

        if (condition.Status == AgentConditionStatus.Detected)
        {
            await ledgerService.TryTransitionAsync(
                condition.Id,
                AgentConditionStatus.Detected,
                AgentConditionStatus.Reported,
                userId: approvedByUserId,
                detail: null,
                fields: approvedByUserId is null
                    ? null
                    : new AgentConditionTransitionFields(ApprovedByUserId: approvedByUserId),
                cancellationToken: cancellationToken);
        }
    }
}
