// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The daily budget and the circuit breaker for one kind in one action tick. Both are counted in the
/// database from the claims' audit events, so several API instances share one budget; the claims this
/// tick has made itself are added on top, because the queries ran before them.
///
/// Counted in the scope they are CONFIGURED in: one bucket per group, plus one for the conditions that
/// carry no group at all. Governance is resolved per group one gate earlier, so a per-kind count would
/// compare a group's own limit against every group's activity and let a busy group exhaust a quiet one.
/// Window counts are cached per window length within a bucket, because governance may configure a
/// different window per scope and the same length must not be re-queried per condition. Lives in its
/// own file rather than nested in AgentConditionActionService so that class stays under its size-guard
/// ceiling.
/// </summary>
/// <param name="repository">Counts the budget-consuming claim events.</param>
/// <param name="triggerKind">The kind whose claims are counted.</param>
/// <param name="nowUtc">The tick's single "now"; the circuit-breaker window ends here.</param>
/// <param name="companyDayStartUtc">Where the daily budget's day began, in the company's own zone.</param>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionBudget
{
    private const string DailyBudgetReason = "the daily action budget of {0} is used up";
    private const string WindowBudgetReason =
        "the circuit breaker tripped - {0} action(s) are allowed per {1} minute(s)";

    private readonly IAgentConditionRepository _repository;
    private readonly string _triggerKind;
    private readonly DateTime _nowUtc;
    private readonly DateTime _companyDayStartUtc;
    private readonly Dictionary<Guid, GroupBudget> _byGroup = new();
    private readonly GroupBudget _installationWide = new();

    public ConditionActionBudget(
        IAgentConditionRepository repository, string triggerKind, DateTime nowUtc, DateTime companyDayStartUtc)
    {
        _repository = repository;
        _triggerKind = triggerKind;
        _nowUtc = nowUtc;
        _companyDayStartUtc = companyDayStartUtc;
    }

    public void RecordClaim(Guid? groupId) => BudgetFor(groupId).ClaimsThisTick++;

    /// <summary>Why this group may not act right now, or null when it may.</summary>
    public async Task<string?> DescribeBlockAsync(
        Guid? groupId, ProactiveGovernanceDecision governance, CancellationToken cancellationToken)
    {
        var budget = BudgetFor(groupId);

        budget.TodayCount ??= await _repository.CountActionClaimsAsync(
            _triggerKind, groupId, _companyDayStartUtc, cancellationToken);

        if (budget.TodayCount.Value + budget.ClaimsThisTick >= governance.DailyActionBudget)
        {
            return string.Format(
                CultureInfo.InvariantCulture, DailyBudgetReason, governance.DailyActionBudget);
        }

        if (!budget.WindowCounts.TryGetValue(governance.WindowMinutes, out var windowCount))
        {
            windowCount = await _repository.CountActionClaimsAsync(
                _triggerKind, groupId, _nowUtc.AddMinutes(-governance.WindowMinutes), cancellationToken);
            budget.WindowCounts[governance.WindowMinutes] = windowCount;
        }

        if (windowCount + budget.ClaimsThisTick >= governance.WindowActionLimit)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                WindowBudgetReason,
                governance.WindowActionLimit,
                governance.WindowMinutes);
        }

        return null;
    }

    /// <summary>
    /// True the first time this group is blocked in this tick. The tick walks on to the other groups
    /// after a block, so without this the same recipient would get one budget report per remaining
    /// candidate of their group.
    /// </summary>
    public bool TryMarkStopReported(Guid? groupId)
    {
        var budget = BudgetFor(groupId);
        if (budget.StopReported)
        {
            return false;
        }

        budget.StopReported = true;
        return true;
    }

    private GroupBudget BudgetFor(Guid? groupId)
    {
        if (groupId is not { } scopedGroupId)
        {
            return _installationWide;
        }

        if (!_byGroup.TryGetValue(scopedGroupId, out var budget))
        {
            budget = new GroupBudget();
            _byGroup[scopedGroupId] = budget;
        }

        return budget;
    }

    private sealed class GroupBudget
    {
        public Dictionary<int, int> WindowCounts { get; } = new();

        public int? TodayCount { get; set; }

        public int ClaimsThisTick { get; set; }

        public bool StopReported { get; set; }
    }
}
