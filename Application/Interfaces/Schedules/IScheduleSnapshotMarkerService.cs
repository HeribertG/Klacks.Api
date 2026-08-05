// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IScheduleSnapshotMarkerService
{
    /// <summary>
    /// Computes the placement fingerprint of the movable schedule rows in scope.
    /// </summary>
    /// <param name="from">First day of the period.</param>
    /// <param name="until">Last day of the period.</param>
    /// <param name="agentIds">Agents in scope.</param>
    /// <param name="analyseToken">Scenario token, null for the real plan.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ScheduleSnapshotMarker> ComputeAsync(
        DateOnly from,
        DateOnly until,
        IReadOnlyList<Guid> agentIds,
        Guid? analyseToken,
        CancellationToken ct = default);
}
