// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Group-visibility boundary of the container template and override endpoints, built on IShiftVisibilityGuard: the
/// container shift must exist and be visible, and a write may only bring in task shifts the caller may see. A hidden
/// container or task shift is answered exactly like a missing one.
/// </summary>
public interface IContainerVisibilityGuard
{
    /// <summary>
    /// True only when the container shift exists and the caller may touch it (one query for every caller).
    /// </summary>
    /// <param name="containerId">Container shift the request is about</param>
    Task<bool> IsContainerVisibleAsync(Guid containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException with the missing-shift text when the container shift is missing or hidden.
    /// </summary>
    /// <param name="containerId">Container shift the request is about</param>
    Task EnsureContainerVisibleAsync(Guid containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException with the missing-shift text when a requested item references a task shift the
    /// caller may not touch and that the container does not reference yet. Task shifts already stored in the
    /// container's templates or overrides are not checked, so a container mixing groups stays saveable; an
    /// unrestricted caller is answered without a query.
    /// </summary>
    /// <param name="containerId">Container shift whose stored items form the baseline</param>
    /// <param name="requestedShiftIds">Task shift ids of the items the write will save (null for absence items)</param>
    Task EnsureItemShiftsVisibleAsync(
        Guid containerId, IEnumerable<Guid?> requestedShiftIds, CancellationToken cancellationToken = default);
}
