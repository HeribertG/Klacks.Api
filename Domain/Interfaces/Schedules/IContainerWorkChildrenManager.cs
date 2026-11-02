// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Manages the business logic for updating container work children (sub-works, sub-breaks, work changes),
/// including macro execution and surcharge calculation.
/// </summary>
/// <param name="workId">The parent container work ID</param>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkChildrenManager
{
    Task<Work?> UpdateChildrenAsync(
        Guid workId,
        string? parentStartBase,
        string? parentEndBase,
        TimeOnly? parentStartTime,
        TimeOnly? parentEndTime,
        List<Work> updatedWorks,
        List<Break> updatedBreaks,
        List<WorkChange> updatedWorkChanges,
        CancellationToken cancellationToken);

    /// <summary>
    /// Recalculates a work after its owner changed, in the order of a save: sub-work macros, then the work with its
    /// unpaid sub-breaks, then the WorkChange macros of the work and its sub-works. Stages only.
    /// </summary>
    /// <param name="parentWork">The tracked work, already carrying the new owner</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>False when a calculation failed; the staged state then mixes both owners and must not be saved</returns>
    Task<bool> RecalculateForNewOwnerAsync(Work parentWork, CancellationToken cancellationToken);
}
