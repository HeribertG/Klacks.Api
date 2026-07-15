// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IEligibilityMatrixBuilder
{
    /// <summary>
    /// Builds the eligibility matrix. <paramref name="preExistingAssignments"/> is the baseline: the
    /// (agent, shift, date) triples the CALLER already holds as an unlocked assignment before this
    /// evaluation. For such a triple, an opt-in QUALIFICATION_EXPIRED_MANDATORY_BLOCKS escalation never
    /// applies (Klacks.Api/Application/Services/Schedules/EligibilityMatrixBuilder.cs) — a pre-existing
    /// expired-mandatory gap stays a Warning so an unrelated re-evaluation cannot retroactively veto an
    /// incumbent; any other agent considered for that same slot is still gated normally.
    /// </summary>
    Task<EligibilityMatrix> BuildAsync(
        IReadOnlyCollection<Guid> agentIds,
        IReadOnlyCollection<EligibilitySlot> slots,
        IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)>? preExistingAssignments = null,
        CancellationToken ct = default);
}
