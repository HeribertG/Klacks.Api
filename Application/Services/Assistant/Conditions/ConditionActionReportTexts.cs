// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The wording of every mandatory report the action dispatcher sends: executed (on an approval of this
/// finding or under a standing approval), failed attempt, escalation, budget stop and withdrawn approval.
/// Pure formatting, invariant culture; who receives a report is decided by ConditionActionOutcomeRecorder.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal static class ConditionActionReportTexts
{
    private const string ExecutedReportFormat =
        "I have carried out a remediation on my own.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nResult: {3}";

    private const string ExecutedUnderStandingApprovalReportFormat =
        "I have carried out a remediation under a standing approval, without asking anybody about this "
        + "finding.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nResult: {3}\n"
        + "Standing approval {4} of user {5}, valid until {6:u}.\n"
        + "If this should not have happened: revoke the standing approval to stop further runs, and ask me "
        + "to roll this change back - there is no one-click undo on this report.";

    private const string FailedReportFormat =
        "A remediation I attempted on my own did not work.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nProblem: {3}\n"
        + "Attempt {4} of {5}; after that I stop trying and leave it to you.";

    private const string EscalatedReportFormat =
        "I am giving up on a finding and leaving it to you.\n\n"
        + "Finding: {0} (condition {1})\nI attempted the remediation {2} time(s) without success.";

    private const string BudgetReportFormat =
        "I stopped acting on '{0}' for now: {1}.\n"
        + "{2} further finding(s) of this kind stay open and unhandled until the limit frees up again.";

    private const string ApprovalWithdrawnReportFormat =
        "An approved remediation was NOT carried out.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nReason: {3}\n"
        + "The finding stays open; I will ask for approval again on the next company day at the earliest.";

    /// <summary>
    /// The mandatory post-action report. Under a standing approval it is a DIFFERENT statement of fact,
    /// not the same sentence with an extra line: nobody was asked about this finding, so the report has to
    /// say under whose advance consent it happened, until when that consent runs, and that there is no
    /// one-click undo on the note itself.
    /// </summary>
    public static string Executed(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        ConditionExecutionAuthority authority,
        string message)
    {
        if (authority.Grant is not { } standing)
        {
            return string.Format(
                CultureInfo.InvariantCulture, ExecutedReportFormat,
                condition.TriggerKind, condition.Id, entry.RemediationSkillName, message);
        }

        return string.Format(
            CultureInfo.InvariantCulture, ExecutedUnderStandingApprovalReportFormat,
            condition.TriggerKind, condition.Id, entry.RemediationSkillName, message,
            standing.Id, standing.GrantedByUserId, standing.ExpiresAtUtc);
    }

    public static string Failed(AgentCondition condition, ConditionRemediationEntry entry, string reason, int attempt) =>
        string.Format(
            CultureInfo.InvariantCulture, FailedReportFormat,
            condition.TriggerKind, condition.Id, entry.RemediationSkillName, reason,
            attempt, AgentConditionActionDefaults.MaxAttemptsBeforeEscalation);

    public static string Escalated(AgentCondition condition) =>
        string.Format(
            CultureInfo.InvariantCulture, EscalatedReportFormat,
            condition.TriggerKind, condition.Id, condition.AttemptCount);

    public static string BudgetStop(string triggerKind, string reason, int remaining) =>
        string.Format(CultureInfo.InvariantCulture, BudgetReportFormat, triggerKind, reason, remaining);

    public static string ApprovalWithdrawn(AgentCondition condition, ConditionRemediationEntry entry, string reason) =>
        string.Format(
            CultureInfo.InvariantCulture, ApprovalWithdrawnReportFormat,
            condition.TriggerKind, condition.Id, entry.RemediationSkillName, reason);
}
