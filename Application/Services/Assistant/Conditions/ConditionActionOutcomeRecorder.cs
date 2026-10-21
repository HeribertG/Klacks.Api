// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes what an action attempt came to - executed, failed, escalated, stopped by a budget - onto the
/// ledger and sends the mandatory report that goes with it. Every outcome detail carries
/// AgentConditionActionDefaults.ActionOutcomeDetailPrefix; claims and approval withdrawals are written
/// elsewhere and carry none.
/// </summary>
/// <param name="ledgerService">Ledger writes: the outcome transitions and the attempt and provenance events.</param>
/// <param name="reporter">Mandatory post-action report, never subject to the notification rate limit.</param>
/// <param name="logger">The dispatcher's logger, so the log category stays the dispatcher's.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionOutcomeRecorder
{
    private const string StandingApprovalDetailFormat = "standingApprovalId={0} grantedByUserId={1}";
    private const string OutcomeDetailFormat = "{0}{1}";
    private const string ExecutedDetail = "executed {0}";
    private const string FailedDetail = "attempt failed: {0}";
    private const string EscalatedDetail = "ineffective after {0} attempt(s)";

    private const string ExecutionNotRecordedMessage =
        "Remediation {Skill} on condition {ConditionId} succeeded but the row had already been "
        + "moved by somebody else; the execution is not recorded on the ledger";

    private const string BudgetStopMessage =
        "Proactive actions on {Kind} stopped: {Reason}. {Remaining} finding(s) stay open this tick";

    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IProactiveActionReporter _reporter;
    private readonly ILogger _logger;

    public ConditionActionOutcomeRecorder(
        IAgentConditionLedgerService ledgerService, IProactiveActionReporter reporter, ILogger logger)
    {
        _ledgerService = ledgerService;
        _reporter = reporter;
        _logger = logger;
    }

    /// <summary>
    /// Moves a successfully remediated row to Executed under the approver, records the standing approval
    /// it ran under (if any) and reports it. A lost transition is logged, not retried: the remediation has
    /// already run, so the report still goes out.
    /// </summary>
    public async Task RecordExecutedAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        ConditionExecutionAuthority authority,
        string message,
        CancellationToken cancellationToken)
    {
        var approverUserId = authority.UserId;
        var recorded = await _ledgerService.TryTransitionAsync(
            condition.Id,
            AgentConditionStatus.Prepared,
            AgentConditionStatus.Executed,
            userId: approverUserId,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, ExecutedDetail, entry.RemediationSkillName)),
            fields: new AgentConditionTransitionFields(
                HandlingKind: AgentConditionHandlingKind.Executed,
                ApprovedByUserId: approverUserId),
            cancellationToken);

        if (!recorded)
        {
            _logger.LogWarning(ExecutionNotRecordedMessage, entry.RemediationSkillName, condition.Id);
        }

        if (authority.Grant is { } standing)
        {
            await _ledgerService.RecordEventAsync(
                condition.Id,
                AgentConditionEventTypes.ExecutedUnderStandingApproval,
                Outcome(string.Format(
                    CultureInfo.InvariantCulture,
                    StandingApprovalDetailFormat, standing.Id, standing.GrantedByUserId)),
                cancellationToken);
        }

        await ReportAsync(
            condition, approverUserId, ConditionActionReportTexts.Executed(condition, entry, authority, message),
            cancellationToken);
    }

    /// <summary>
    /// Records a failed attempt WITHOUT moving the row: it stays Prepared so the stale-claim path can
    /// take it over on a later tick, and AttemptCount - already raised by the claim - carries it towards
    /// escalation. The report goes out for a failure too; a report only on success would let a
    /// remediation fail three times in silence before anybody hears about it.
    /// </summary>
    public async Task RecordFailureAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        Guid approverUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var attempt = condition.AttemptCount + 1;

        await _ledgerService.RecordEventAsync(
            condition.Id,
            AgentConditionEventTypes.AttemptFailed,
            Outcome(string.Format(CultureInfo.InvariantCulture, FailedDetail, reason)),
            cancellationToken);

        await ReportAsync(
            condition, approverUserId, ConditionActionReportTexts.Failed(condition, entry, reason, attempt),
            cancellationToken);
    }

    /// <summary>
    /// Hands one row to a human after MaxAttemptsBeforeEscalation attempts. Returns whether THIS caller
    /// escalated it: a lost compare-and-swap means another instance got there first, and counting it
    /// anyway would over-report the one number a planner reads to spot a stuck kind.
    /// </summary>
    public async Task<bool> EscalateAsync(AgentCondition condition, Guid? approverUserId, CancellationToken cancellationToken)
    {
        var escalated = await _ledgerService.TryTransitionAsync(
            condition.Id,
            condition.Status,
            AgentConditionStatus.Escalated,
            userId: approverUserId,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, EscalatedDetail, condition.AttemptCount)),
            cancellationToken: cancellationToken);

        if (!escalated)
        {
            return false;
        }

        await ReportAsync(condition, approverUserId, ConditionActionReportTexts.Escalated(condition), cancellationToken);

        return true;
    }

    /// <summary>
    /// A budget stop is never silent: it is logged on EVERY tick it happens. The durable note is
    /// deliberately narrower - only on a tick that actually acted - because once a day's budget is spent
    /// every remaining tick of that day stops on its first candidate, and the tick on which the budget
    /// ran out is the one that carries the information.
    /// </summary>
    public async Task ReportBudgetStopAsync(
        ConditionActionKindRun run,
        AgentCondition condition,
        Guid? approverUserId,
        string reason,
        int remaining,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(BudgetStopMessage, run.TriggerKind, reason, remaining);

        if (run.ActionsThisTick == 0)
        {
            return;
        }

        await ReportAsync(
            condition, approverUserId, ConditionActionReportTexts.BudgetStop(run.TriggerKind, reason, remaining),
            cancellationToken);
    }

    /// <summary>
    /// Who hears about this row (Owner decision 2026-09-20): the approver plus the finding's planning
    /// audience (or the admins without a group); before any approval exists the audience alone.
    /// </summary>
    public Task ReportAsync(
        AgentCondition condition, Guid? approverUserId, string message, CancellationToken cancellationToken) =>
        _reporter.ReportToApprovalAudienceAsync(approverUserId, condition.GroupId, message, cancellationToken);

    private static string Outcome(string detail) =>
        string.Format(
            CultureInfo.InvariantCulture,
            OutcomeDetailFormat,
            AgentConditionActionDefaults.ActionOutcomeDetailPrefix,
            detail);
}
