// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    /// <param name="includeFrozen">Admin override: the cached period hours of frozen periods are recalculated too;
    /// without it they stay unchanged</param>
    /// <param name="ownWorkChangesOnly">Restricted caller (planner): only work changes on works of the scoped clients are
    /// reprocessed, not those where a scoped client merely replaces a person outside the scope (owner decision 2026-11-08)</param>
    bool QueueRecalculation(
        DateOnly startDate,
        DateOnly endDate,
        Guid? selectedGroup,
        Guid? analyseToken,
        IReadOnlyCollection<Guid>? clientIds = null,
        bool includeFrozen = false,
        bool ownWorkChangesOnly = false);

    /// <summary>
    /// Enqueues a thorough recalculation of every unsealed real-mode work and every unsealed break, whatever its date.
    /// The service finds the first and last day itself and works month by month. Returns false when the queue is full.
    /// </summary>
    bool QueueRecalculationOfAllUnsealed();
}
