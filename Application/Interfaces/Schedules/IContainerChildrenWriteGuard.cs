// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// What a container save has to pass for its children that a write of the same row on its own passes as well: the stored
/// seal state, scenario and clone link stay with the stored row, the lock level and the day locks of old and new data are
/// respected, and a child that is left out is deleted only where a delete would be allowed.
/// </summary>
public interface IContainerChildrenWriteGuard
{
    /// <summary>
    /// Applies the server-owned state to the rebuilt children and refuses what the caller may not write. The seal state
    /// (lock level, seal data, pre-seal data, group of a seal), the scenario token and the clone link of a child that is
    /// stored come from the stored row, those of a new child from the container; every child and work change is attached
    /// to the container's scenario. Throws KeyNotFoundException, answered like a missing container, when a new child takes
    /// an id a row has taken already; InvalidRequestException when a lock level or a sealed day forbids the write.
    /// </summary>
    /// <param name="container">The container work being saved, as stored</param>
    /// <param name="stored">The children as stored</param>
    /// <param name="works">Sub-works rebuilt from the request; changed in place</param>
    /// <param name="breaks">Sub-breaks rebuilt from the request; changed in place</param>
    /// <param name="workChanges">Work changes rebuilt from the request; changed in place</param>
    Task PrepareAsync(
        Work container,
        StoredContainerChildren stored,
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WorkChange> workChanges,
        CancellationToken cancellationToken);
}
