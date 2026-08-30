// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns resolved governance decisions into the transport shape shared by the read query, the write
/// command and the settings card, so both handlers answer with exactly the same picture.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

public static class ProactiveGovernanceDtoMapper
{
    public static ProactiveGovernanceDto ToDto(
        bool killSwitchActive, AutonomyLevel globalAutonomyLevel,
        IReadOnlyList<ProactiveGovernanceDecision> decisions)
    {
        return new ProactiveGovernanceDto
        {
            KillSwitchActive = killSwitchActive,
            GlobalAutonomyLevel = (int)globalAutonomyLevel,
            GlobalAutonomyCap = (int)ProactiveGovernanceDefaults.MapAutonomyLevel(globalAutonomyLevel),
            Rules = decisions.Select(ToRuleDto).ToList()
        };
    }

    private static ProactiveGovernanceRuleDto ToRuleDto(ProactiveGovernanceDecision decision)
    {
        return new ProactiveGovernanceRuleDto
        {
            TriggerKind = decision.TriggerKind,
            GroupId = decision.GroupId,
            MaxAction = (int)decision.ConfiguredMaxAction,
            MaxActionName = decision.ConfiguredMaxAction.ToString(),
            EffectiveMaxAction = (int)decision.EffectiveMaxAction,
            GlobalAutonomyCap = (int)decision.GlobalAutonomyCap,
            Enabled = decision.Enabled,
            ResponsibleOwnerUserId = decision.ResponsibleOwnerUserId,
            DailyActionBudget = decision.DailyActionBudget,
            WindowActionLimit = decision.WindowActionLimit,
            WindowMinutes = decision.WindowMinutes,
            IsStored = decision.IsStored
        };
    }
}
