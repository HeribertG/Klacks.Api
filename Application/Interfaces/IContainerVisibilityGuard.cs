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
    /// Throws KeyNotFoundException with the missing-shift text when the container shift is missing or hidden (one
    /// existence-and-visibility query for every caller).
    /// </summary>
    /// <param name="containerId">Container shift the request is about</param>
    Task EnsureContainerVisibleAsync(Guid containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The check of every write to the templates and overrides of a container: EnsureContainerVisibleAsync, and the same
    /// missing-shift answer for a container of a variant (key), for every caller. A key carries shifts only, so a real
    /// template or override row on its container would get past the key freeze and outlive the key.
    /// </summary>
    /// <param name="containerId">Container shift the write is about</param>
    Task EnsureContainerWritableAsync(Guid containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException with the missing-shift text when a requested item references a task shift the
    /// caller may not touch and that the container does not reference yet. Task shifts already stored in the
    /// container's templates or overrides are not checked, so a container mixing groups stays saveable. A task shift of
    /// a variant (key) is refused with the same text for every caller, stored or not; scenario clones are left to the
    /// expansion, which knows the token of the container work. An unrestricted caller is otherwise answered without
    /// a visibility query.
    /// </summary>
    /// <param name="containerId">Container shift whose stored items form the baseline</param>
    /// <param name="requestedShiftIds">Task shift ids of the items the write will save (null for absence items)</param>
    Task EnsureItemShiftsVisibleAsync(
        Guid containerId, IEnumerable<Guid?> requestedShiftIds, CancellationToken cancellationToken = default);
}
