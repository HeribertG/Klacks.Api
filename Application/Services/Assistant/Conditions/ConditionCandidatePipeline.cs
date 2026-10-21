// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One candidate row through the action dispatcher's gates, in the order AgentConditionActionService
/// documents: effective MaxAction, cascade guard, attempt limit, quiet window, pre-flight binding, stale
/// approval, per-tick cap, daily budget and circuit breaker - and only then the approver, the standing
/// approval or the approval chain. The order is the safety; every step is a literal copy of the
/// dispatcher's former gate sequence.
/// </summary>
/// <param name="governance">Gate (0): the effective MaxAction of the row.</param>
/// <param name="cascadeGuard">Gate (1): rows an earlier Klacksy execution may have produced.</param>
/// <param name="quietWindow">Answers whether now is a bad moment to touch this condition's target.</param>
/// <param name="argumentBinder">Pre-flight binding, so an unbindable row costs neither an attempt nor budget.</param>
/// <param name="approvalHandler">Stale-approval withdrawal and the approval chain request.</param>
/// <param name="outcomeRecorder">Escalation and the budget-stop report.</param>
/// <param name="executor">Claims and executes a row that carries an authority.</param>
/// <param name="standingApprovalGate">An administrator's advance approval for the kind and scope.</param>
/// <param name="logger">The dispatcher's logger, so the log category stays the dispatcher's.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionCandidatePipeline
{
    private const string TickCapReason =
        "the absolute cap of {0} action(s) per kind per scan is reached";

    private const string PrepareWithoutScenarioMessage =
        "Kind {Kind} is governed at Prepare, but its remediation {Skill} is execute-only and cannot be "
        + "staged as a scenario; condition {ConditionId} is reported and left to a human";

    private readonly ConditionActionGovernance _governance;
    private readonly ConditionCascadeGuard _cascadeGuard;
    private readonly IQuietWindowService _quietWindow;
    private readonly ConditionRemediationArgumentBinder _argumentBinder;
    private readonly ConditionApprovalHandler _approvalHandler;
    private readonly ConditionActionOutcomeRecorder _outcomeRecorder;
    private readonly ConditionRemediationExecutor _executor;
    private readonly ConditionStandingApprovalGate _standingApprovalGate;
    private readonly ILogger _logger;

    public ConditionCandidatePipeline(
        ConditionActionGovernance governance,
        ConditionCascadeGuard cascadeGuard,
        IQuietWindowService quietWindow,
        ConditionRemediationArgumentBinder argumentBinder,
        ConditionApprovalHandler approvalHandler,
        ConditionActionOutcomeRecorder outcomeRecorder,
        ConditionRemediationExecutor executor,
        ConditionStandingApprovalGate standingApprovalGate,
        ILogger logger)
    {
        _governance = governance;
        _cascadeGuard = cascadeGuard;
        _quietWindow = quietWindow;
        _argumentBinder = argumentBinder;
        _approvalHandler = approvalHandler;
        _outcomeRecorder = outcomeRecorder;
        _executor = executor;
        _standingApprovalGate = standingApprovalGate;
        _logger = logger;
    }

    /// <summary>
    /// One candidate through the gates. Returns true when the kind must stop for this tick (the absolute
    /// per-tick cap), false to walk on to the next row - including after a group's budget block, because
    /// the candidates behind it may belong to groups that have spent nothing.
    /// </summary>
    public async Task<bool> ProcessAsync(ConditionActionKindRun run, int index, CancellationToken cancellationToken)
    {
        var condition = run.Candidates[index];
        var governance = await _governance.ResolveAsync(run.GovernanceCache, condition, cancellationToken);
        if (!IsInActionBranch(run, condition, governance))
        {
            return false;
        }

        if (await _cascadeGuard.IsCascadeAsync(condition, run.RecentExecutions, cancellationToken))
        {
            run.Tally.SkippedCascade++;
            return false;
        }

        var approver = ResolveApprover(condition);
        var arguments = await PassRowGatesAsync(run, condition, approver, cancellationToken);
        if (arguments is null)
        {
            return false;
        }

        if (run.ActionsThisTick >= AgentConditionActionDefaults.MaxExecutionsPerKindPerTick)
        {
            await StopForTickCapAsync(run, index, condition, approver, cancellationToken);
            return true;
        }

        if (await IsBudgetBlockedAsync(run, index, condition, approver, governance, cancellationToken))
        {
            return false;
        }

        await ActAsync(run, condition, approver, arguments, cancellationToken);
        return false;
    }

    /// <summary>
    /// Whose rights this row would run under: the stamped approver - the approval chain's or the
    /// delegation's answer - or null when nobody has released it yet.
    /// </summary>
    private static Guid? ResolveApprover(AgentCondition condition) =>
        condition.ApprovedByUserId is Guid approverUserId && approverUserId != Guid.Empty ? approverUserId : null;

    private bool IsInActionBranch(
        ConditionActionKindRun run, AgentCondition condition, ProactiveGovernanceDecision governance)
    {
        var maxAction = _governance.EffectiveMaxActionFor(governance, condition);
        if (maxAction >= ProactiveMaxAction.Execute)
        {
            return true;
        }

        if (maxAction == ProactiveMaxAction.Prepare && !run.Entry.IsScenarioCapable)
        {
            _logger.LogDebug(
                PrepareWithoutScenarioMessage, run.TriggerKind, run.Entry.RemediationSkillName, condition.Id);
        }

        return false;
    }

    /// <summary>
    /// Gates (2) and (3) plus the pre-flight binding and the stale-approval withdrawal. Returns the
    /// remediation's arguments, or null when the row is passed over this tick.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>?> PassRowGatesAsync(
        ConditionActionKindRun run, AgentCondition condition, Guid? approver, CancellationToken cancellationToken)
    {
        if (condition.AttemptCount >= AgentConditionActionDefaults.MaxAttemptsBeforeEscalation)
        {
            if (await _outcomeRecorder.EscalateAsync(condition, approver, cancellationToken))
            {
                run.Tally.Escalated++;
            }

            return null;
        }

        if (await _quietWindow.IsQuietForAsync(condition, cancellationToken))
        {
            run.Tally.SkippedQuiet++;
            return null;
        }

        var arguments = _argumentBinder.TryBind(run.Entry, condition);
        if (arguments is null)
        {
            run.Tally.SkippedUnbindable++;
            return null;
        }

        if (approver is Guid freshApprover
            && condition.Status == AgentConditionStatus.Reported
            && await _approvalHandler.WithdrawIfApprovalIsStaleAsync(run, condition, freshApprover, cancellationToken))
        {
            return null;
        }

        return arguments;
    }

    private async Task StopForTickCapAsync(
        ConditionActionKindRun run, int index, AgentCondition condition, Guid? approver, CancellationToken cancellationToken)
    {
        var remaining = run.Candidates.Count - index;
        await _outcomeRecorder.ReportBudgetStopAsync(
            run, condition, approver,
            string.Format(CultureInfo.InvariantCulture, TickCapReason, AgentConditionActionDefaults.MaxExecutionsPerKindPerTick),
            remaining, cancellationToken);
        run.Tally.LeftForBudget += remaining;
    }

    private async Task<bool> IsBudgetBlockedAsync(
        ConditionActionKindRun run,
        int index,
        AgentCondition condition,
        Guid? approver,
        ProactiveGovernanceDecision governance,
        CancellationToken cancellationToken)
    {
        var blockedReason = await run.Budget.DescribeBlockAsync(condition.GroupId, governance, cancellationToken);
        if (blockedReason is null)
        {
            return false;
        }

        if (run.Budget.TryMarkStopReported(condition.GroupId))
        {
            var leftInGroup = run.Candidates.Skip(index).Count(remaining => remaining.GroupId == condition.GroupId);
            await _outcomeRecorder.ReportBudgetStopAsync(run, condition, approver, blockedReason, leftInGroup, cancellationToken);
            run.Tally.LeftForBudget += leftInGroup;
        }

        return true;
    }

    /// <summary>
    /// The row has passed every gate: run it under its stamped approver, or under a standing approval
    /// that covers it, or ask for its approval chain - in that order.
    /// </summary>
    private async Task ActAsync(
        ConditionActionKindRun run,
        AgentCondition condition,
        Guid? approver,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        if (approver is Guid approverUserId)
        {
            await _executor.ClaimAndExecuteAsync(
                run, condition, new ConditionExecutionAuthority(approverUserId), arguments, cancellationToken);
            return;
        }

        var standing = await _standingApprovalGate.TryApplyAsync(
            condition, run.Entry, run.Budget, run.NowUtc, cancellationToken);

        if (standing.Authority is { } grantedAuthority)
        {
            await _executor.ClaimAndExecuteAsync(run, condition, grantedAuthority, arguments, cancellationToken);
        }
        else if (standing.AskForApproval)
        {
            await _approvalHandler.AskForApprovalOrSkipAsync(run, condition, cancellationToken);
        }
    }
}
