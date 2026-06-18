// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Loads agent availability for the given shift slots and returns the (agent, shift, date) triples the
/// agents are explicitly unavailable for — the C2 availability gate, ready to union into a wizard's
/// IneligibleAssignments. Additive: sparse availability means available, so it never removes a valid
/// assignment. One DB load per call; the matching is the pure <see cref="AvailabilityMatrixBuilder"/>.
/// </summary>
public interface IAvailabilityIneligibilityService
{
    Task<IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)>> GetAsync(
        IReadOnlyList<Guid> agentIds,
        IReadOnlyList<AvailabilityShiftSlot> slots,
        CancellationToken ct);
}
