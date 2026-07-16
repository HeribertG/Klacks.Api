// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Queue for thorough recalculation requests processed by the background service.
/// </summary>
public interface IThoroughRecalculationQueue
{
    /// <summary>
    /// Enqueues a thorough recalculation of the given date window. Returns false when the queue is full.
    /// </summary>
    /// <param name="startDate">First day of the recalculation window</param>
    /// <param name="endDate">Last day of the recalculation window</param>
    /// <param name="selectedGroup">Optional group whose clients (including subgroups) limit the recalculation</param>
    /// <param name="analyseToken">Scenario token; null recalculates real-mode entries</param>
    /// <param name="clientIds">Optional client scope; null or empty recalculates all clients in the window</param>
    bool QueueRecalculation(
        DateOnly startDate,
        DateOnly endDate,
        Guid? selectedGroup,
        Guid? analyseToken,
        IReadOnlyCollection<Guid>? clientIds = null);
}
