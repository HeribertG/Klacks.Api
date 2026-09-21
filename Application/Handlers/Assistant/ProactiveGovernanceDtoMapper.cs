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
/// can tell an asked-for level from a reachable one.
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
            Rules = decisions.Select(decision => ToRuleDto(decision, remediationRegistry)).ToList()
        };
    }

    private static ProactiveGovernanceRuleDto ToRuleDto(
        ProactiveGovernanceDecision decision, IConditionRemediationRegistry remediationRegistry)
    {
        return new ProactiveGovernanceRuleDto
        {
            TriggerKind = decision.TriggerKind,
            GroupId = decision.GroupId,
            MaxAction = (int)decision.ConfiguredMaxAction,
            MaxActionName = decision.ConfiguredMaxAction.ToString(),
            EffectiveMaxAction = (int)remediationRegistry.TryGetEffectiveMaxAction(
                decision.TriggerKind, decision.EffectiveMaxAction),
            GlobalAutonomyCap = (int)decision.GlobalAutonomyCap,
            IsScenarioCapable = IsScenarioCapable(remediationRegistry, decision.TriggerKind),
            Enabled = decision.Enabled,
            DailyActionBudget = decision.DailyActionBudget,
            WindowActionLimit = decision.WindowActionLimit,
            WindowMinutes = decision.WindowMinutes,
            IsStored = decision.IsStored
        };
    }

    private static bool IsScenarioCapable(IConditionRemediationRegistry remediationRegistry, string triggerKind) =>
        remediationRegistry.TryGetEntry(triggerKind, out var entry) && entry is { IsScenarioCapable: true };
}
