// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IWorkHandOverService
{
    /// <summary>
    /// Gives a tracked work, its container children and everything hanging on them to another person and recalculates
    /// what depends on the owner's contract. Stages only: the caller commits, then runs the overtime successors and the
    /// period hours of both people.
    /// </summary>
    /// <param name="work">The tracked work</param>
    /// <param name="targetClientId">The new owner</param>
    /// <param name="movedFromClientId">Person an absence cover took the work from; null keeps the stored marker</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>False when the recalculation failed; the caller must then not save the staged state</returns>
    Task<bool> TransferAsync(Work work, Guid targetClientId, Guid? movedFromClientId, CancellationToken cancellationToken);
}
