// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The approval side of the action dispatcher (approval chain, design 2026-09-20): asks for a chain for a
/// row nobody has released, withdraws an approval that went stale before the tick reached it, and
/// withdraws one whose approver no longer qualifies. A withdrawal is reported; a lost withdrawal is not.
/// </summary>
/// <param name="approvalStarter">Opens the approval chain for an executable row that nobody has approved yet.</param>
/// <param name="ledgerService">Clears the approval stamp and writes the withdrawal event.</param>
/// <param name="outcomeRecorder">Sends the withdrawal report to the approver and the finding's audience.</param>
/// <param name="logger">The dispatcher's logger, so the log category stays the dispatcher's.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionApprovalHandler
{
    private const string ApprovalExpiredDetail = "approval older than {0} minute(s) when the tick reached the row";

    private const string NoApproverForClaimedRowMessage =
        "Condition {ConditionId} of kind {Kind} is claimed but carries no approval, so it is left to escalate";

    private readonly IConditionApprovalChainStarter _approvalStarter;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly ConditionActionOutcomeRecorder _outcomeRecorder;
    private readonly ILogger _logger;

    public ConditionApprovalHandler(
        IConditionApprovalChainStarter approvalStarter,
        IAgentConditionLedgerService ledgerService,
        ConditionActionOutcomeRecorder outcomeRecorder,
        ILogger logger)
    {
        _approvalStarter = approvalStarter;
        _ledgerService = ledgerService;
        _outcomeRecorder = outcomeRecorder;
        _logger = logger;
    }

    /// <summary>
    /// A Reported row with nobody behind it gets its approval chain asked for; a Prepared row with nobody
    /// behind it is a claim this regime cannot resume (no approval to run under) and is left to age into
    /// escalation. Every non-Started outcome of the starter is fail closed: counted, logged by the starter
    /// itself, and never retried within this tick. A started chain counts against the per-tick cap so one
    /// busy kind cannot flood a planner's inbox with requests in a single scan.
    /// </summary>
    public async Task AskForApprovalOrSkipAsync(
        ConditionActionKindRun run, AgentCondition condition, CancellationToken cancellationToken)
    {
        if (condition.Status != AgentConditionStatus.Reported)
        {
            _logger.LogDebug(NoApproverForClaimedRowMessage, condition.Id, condition.TriggerKind);
            run.Tally.SkippedNoApprover++;
            return;
        }

        var outcome = await _approvalStarter.TryStartAsync(condition, run.Entry, run.CompanyDayStartUtc, cancellationToken);
        switch (outcome)
        {
            case ConditionApprovalStartOutcome.Started:
                run.Tally.ApprovalsRequested++;
                run.RecordAction();
                break;
            case ConditionApprovalStartOutcome.ChainAlreadyRunning:
            case ConditionApprovalStartOutcome.WaitingForNextCompanyDay:
                run.Tally.AwaitingApproval++;
                break;
            default:
                run.Tally.ApprovalsUnavailable++;
                break;
        }
    }

    /// <summary>
    /// The execution window of a fresh approval: a stamp older than ApprovalExecutionWindowMinutes when
    /// the tick reaches the row is withdrawn rather than acted on, because the finding may no longer be
    /// what the approver looked at. The window is measured in scan intervals, not in the stale-claim
    /// minutes: an approval is answered by a LATER tick, so it must survive at least the one tick that may
    /// legitimately pass the row over. Returns true when the row was withdrawn (or the withdrawal was lost
    /// to a concurrent tick), in which case the caller must not act on it.
    /// </summary>
    public async Task<bool> WithdrawIfApprovalIsStaleAsync(
        ConditionActionKindRun run, AgentCondition condition, Guid approverUserId, CancellationToken cancellationToken)
    {
        var freshAfterUtc = run.NowUtc.AddMinutes(-AgentConditionActionDefaults.ApprovalExecutionWindowMinutes);
        if (condition.ApprovedAtUtc is { } approvedAtUtc && approvedAtUtc >= freshAfterUtc)
        {
            return false;
        }

        await WithdrawApprovalAsync(
            run, condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, ApprovalExpiredDetail, AgentConditionActionDefaults.ApprovalExecutionWindowMinutes),
            cancellationToken);
        return true;
    }

    public async Task WithdrawApprovalAsync(
        ConditionActionKindRun run,
        AgentCondition condition,
        Guid approverUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var withdrawn = await _ledgerService.TryWithdrawApprovalAsync(condition.Id, approverUserId, reason, cancellationToken);
        if (!withdrawn)
        {
            return;
        }

        run.Tally.ApprovalsWithdrawn++;
        await _outcomeRecorder.ReportAsync(
            condition, approverUserId,
            ConditionActionReportTexts.ApprovalWithdrawn(condition, run.Entry, reason),
            cancellationToken);
    }
}
