// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The condition-ledger status set a human planner still needs to hear about: Detected, Reported,
/// Prepared and Escalated. Deliberately NOT AgentConditionStateMachine.OpenStatuses, which excludes
/// Escalated for an unrelated reason (re-arm eligibility for the partial unique index on Fingerprint,
/// see that type's own remarks) - excluding Escalated here would hide exactly the findings that most
/// need a planner's attention. Shared by AgentConditionRepository.GetTopForContextAsync (Etappe 3g, the
/// per-turn context block, further capped to High/Medium severity there) and
/// AgentConditionRepository.GetOpenForScopeAsync/CountOpenForScopeAsync (Etappe 3f, the list_open_findings
/// chat skill), so the two "what does a planner still need to know" surfaces agree by construction
/// instead of by two independent call sites happening to list the same four values.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionPlannerRelevantStatuses
{
    public static readonly IReadOnlyList<AgentConditionStatus> Values =
    [
        AgentConditionStatus.Detected,
        AgentConditionStatus.Reported,
        AgentConditionStatus.Prepared,
        AgentConditionStatus.Escalated
    ];
}
