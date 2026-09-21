// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Start request for a ProactiveApproval chain: unlike StartEscalationChainRequest the caller has already
/// resolved the ordered roster (IConditionApprovalRosterResolver) and owns the deadline
/// (ProactiveApprovalDeadline or the kind's natural anchor), so the chain service neither consults the
/// absence roster nor applies the prep-buffer or urgency arithmetic.
/// </summary>
/// <param name="ConditionId">The agent_conditions row awaiting approval; at most one Running chain per condition.</param>
/// <param name="GroupId">The condition's group, kept on the chain for the intervention list; null for a finding without group.</param>
/// <param name="Roster">Ordered, already rights-filtered candidates; frozen into stages in this order.</param>
/// <param name="DeadlineUtc">Absolute deadline the wave calculator distributes the stages against.</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public readonly record struct StartConditionApprovalChainRequest(
    Guid ConditionId,
    Guid? GroupId,
    IReadOnlyList<EscalationRosterCandidate> Roster,
    DateTime DeadlineUtc);
