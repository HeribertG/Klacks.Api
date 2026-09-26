// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns resolved governance decisions into the transport shape shared by the read query, the write
/// command and the settings card, so both handlers answer with exactly the same picture. The
/// remediation registry is consulted here rather than folded into the decision, because it is a
/// code-only gate the governance resolver deliberately knows nothing about.
///
/// EffectiveMaxAction is reported through that gate, not straight from the decision. The gate is what
/// the dispatching tick actually obeys (AgentConditionActionService), so reporting the ungated level
/// made the settings card offer an Execute or Prepare that the tick would then cap at Hint and do
/// nothing with. The configured level is still reported unchanged beside it as MaxAction, so a reader
/// can tell an asked-for level from a reachable one. period_auto_close is the one exception: its own chain,
/// not the tick, obeys the rule, so its effective level is reported as that chain applies it.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

public static class ProactiveGovernanceDtoMapper
{
    public static ProactiveGovernanceDto ToDto(
        bool killSwitchActive, AutonomyLevel globalAutonomyLevel,
        IReadOnlyList<ProactiveGovernanceDecision> decisions,
        IConditionRemediationRegistry remediationRegistry)
    {
        return new ProactiveGovernanceDto
        {
            KillSwitchActive = killSwitchActive,
            GlobalAutonomyLevel = (int)globalAutonomyLevel,
            GlobalAutonomyCap = (int)ProactiveGovernanceDefaults.MapAutonomyLevel(globalAutonomyLevel),
            Rules = decisions.Select(decision => ToRuleDto(decision, globalAutonomyLevel, remediationRegistry)).ToList()
        };
    }

    private static ProactiveGovernanceRuleDto ToRuleDto(
        ProactiveGovernanceDecision decision,
        AutonomyLevel globalAutonomyLevel,
        IConditionRemediationRegistry remediationRegistry)
    {
        return new ProactiveGovernanceRuleDto
        {
            TriggerKind = decision.TriggerKind,
            GroupId = decision.GroupId,
            MaxAction = (int)decision.ConfiguredMaxAction,
            MaxActionName = decision.ConfiguredMaxAction.ToString(),
            EffectiveMaxAction = (int)EffectiveMaxActionFor(decision, globalAutonomyLevel, remediationRegistry),
            GlobalAutonomyCap = (int)decision.GlobalAutonomyCap,
            IsScenarioCapable = IsScenarioCapable(remediationRegistry, decision.TriggerKind),
            Enabled = decision.Enabled,
            DailyActionBudget = decision.DailyActionBudget,
            WindowActionLimit = decision.WindowActionLimit,
            WindowMinutes = decision.WindowMinutes,
            BudgetApplies = ProactiveGovernanceDefaults.BudgetApplies(decision.TriggerKind),
            FixedRunLimit = ProactiveGovernanceDefaults.FixedRunLimitFor(decision.TriggerKind),
            IsStored = decision.IsStored
        };
    }

    /// <summary>
    /// period_auto_close is not acted on by the dispatching tick but by its own chain (PeriodAutoCloseResolver /
    /// PeriodAutoCloseService), which checks kill switch and Enabled, then the rule's configured MaxAction, and
    /// seals only on Execute with the global level at FullyAutonomous - for this display the same as an
    /// EffectiveMaxAction of Execute under that level. Its effective level is therefore reported as that chain
    /// obeys it - Execute exactly then, Hint otherwise (Prepare does nothing there) - and not through the
    /// remediation registry, which knows no entry for it and would always show Hint while closes run. The
    /// per-admin autonomy minimum is not part of this rule-level value. Every other kind, next_period_scheduling_due
    /// included, keeps the registry-gated value.
    /// </summary>
    private static ProactiveMaxAction EffectiveMaxActionFor(
        ProactiveGovernanceDecision decision,
        AutonomyLevel globalAutonomyLevel,
        IConditionRemediationRegistry remediationRegistry)
    {
        if (string.Equals(decision.TriggerKind, AgentTriggerKinds.PeriodAutoClose, StringComparison.Ordinal))
        {
            return decision.EffectiveMaxAction == ProactiveMaxAction.Execute
                && globalAutonomyLevel == AutonomyLevel.FullyAutonomous
                ? ProactiveMaxAction.Execute
                : ProactiveMaxAction.Hint;
        }

        return remediationRegistry.TryGetEffectiveMaxAction(decision.TriggerKind, decision.EffectiveMaxAction);
    }

    private static bool IsScenarioCapable(IConditionRemediationRegistry remediationRegistry, string triggerKind) =>
        remediationRegistry.TryGetEntry(triggerKind, out var entry) && entry is { IsScenarioCapable: true };
}
