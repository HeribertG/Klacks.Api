// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Deletes a whole sealed order family (the order, its plannable copy and every cut piece) for an administrator. It does
/// not check the role: the caller (the shift delete handler, which only the administrator reaches through REST, or the
/// delete_shift skill) must have established that the user is an administrator.
/// </summary>
public interface ISealedOrderFamilyDeletion
{
    /// <summary>
    /// Soft-deletes the family and commits. Throws ShiftLockedException when a work, a scenario clone or a container
    /// reference still exists on any family shift; nothing is deleted in that case.
    /// </summary>
    /// <param name="sealedOrder">The sealed order that heads the family</param>
    /// <param name="cancellationToken">Cancels the usage query</param>
    /// <returns>The number of shifts that were deleted</returns>
    Task<int> DeleteAsync(Shift sealedOrder, CancellationToken cancellationToken = default);
}
