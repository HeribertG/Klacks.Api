// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Limits the shift ids a caller hands to the route optimization to the ones the caller may read, because the route
/// answers with customer names and addresses. A real shift must be visible to the caller. The container dialog of a
/// schedule sends the clones of the open scenario (the sub-works of a cloned container work point at them), so a clone
/// stays when its scenario is visible. A shift of a variant (key) is left out for every caller: planning runs in
/// scenarios only.
/// </summary>
public interface IRouteShiftVisibilityFilter
{
    /// <summary>
    /// Filters the given shift ids down to those the caller may read.
    /// </summary>
    /// <param name="shiftIds">Shift ids of the request</param>
    /// <returns>The distinct ids the caller may read, in request order; a missing id is dropped later by the shift read</returns>
    Task<List<Guid>> FilterAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);
}
