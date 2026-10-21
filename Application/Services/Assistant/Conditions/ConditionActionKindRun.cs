// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The state of the action dispatcher for ONE kind in one tick. Everything that must not leak from one
/// kind into the next lives here and nowhere else: the governance cache is keyed by group only, the
/// budget counts one kind's claims and remembers which group's stop it has already reported, and
/// ActionsThisTick is the per-kind cap's counter. A fresh instance per kind is what keeps a Hint kind from
/// borrowing an Execute kind's decision and one busy kind from spending another kind's cap. A class, not a
/// record, because the collaborators count actions through RecordAction.
/// </summary>
/// <param name="triggerKind">The kind this run walks.</param>
/// <param name="entry">The kind's remediation from the code-only registry.</param>
/// <param name="nowUtc">The tick's single "now"; no collaborator reads the clock itself.</param>
/// <param name="companyDayStartUtc">Where the company's own day began, for the budget and the approval chain.</param>
/// <param name="recentExecutions">Klacksy's executions within one scan interval, for the cascade guard.</param>
/// <param name="candidates">The kind's actionable rows, most severe and oldest first.</param>
/// <param name="budget">The kind's daily budget and circuit breaker, per group.</param>
/// <param name="tally">The tick-wide counters, shared by every kind of the tick.</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionKindRun
{
    public ConditionActionKindRun(
        string triggerKind,
        ConditionRemediationEntry entry,
        DateTime nowUtc,
        DateTime companyDayStartUtc,
        IReadOnlyList<AgentCondition> recentExecutions,
        IReadOnlyList<AgentCondition> candidates,
        ConditionActionBudget budget,
        ConditionActionTally tally)
    {
        TriggerKind = triggerKind;
        Entry = entry;
        NowUtc = nowUtc;
        CompanyDayStartUtc = companyDayStartUtc;
        RecentExecutions = recentExecutions;
        Candidates = candidates;
        Budget = budget;
        Tally = tally;
    }

    public string TriggerKind { get; }

    public ConditionRemediationEntry Entry { get; }

    public DateTime NowUtc { get; }

    public DateTime CompanyDayStartUtc { get; }

    public IReadOnlyList<AgentCondition> RecentExecutions { get; }

    public IReadOnlyList<AgentCondition> Candidates { get; }

    public ConditionActionBudget Budget { get; }

    public ConditionActionTally Tally { get; }

    public ConditionGovernanceCache GovernanceCache { get; } = new();

    public int ActionsThisTick { get; private set; }

    /// <summary>Counts one action against the per-kind tick cap: a successful claim or a started approval chain.</summary>
    public void RecordAction() => ActionsThisTick++;
}
