// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule that a period can only be closed once every service of it is sealed (LockLevel Confirmed or higher) and
/// no service of it lies on a stand-in. Shared by the whole-period close, the group close and the automatic close, so the
/// three cannot drift apart.
/// Absences do not take part: they are sealed together with the close.
/// </summary>

namespace Klacks.Api.Application.Interfaces.PeriodClosing;

public interface IPeriodSealCompletenessGuard
{
    /// <summary>
    /// Number of unsealed top-level works of the real plan in the period. A group close counts only the works on the
    /// group's own shifts, so works of other groups never block it.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> CountUnsealedAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of top-level works of the real plan in the period that lie on a stand-in, over the same population as
    /// CountUnsealedAsync.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> CountStandInWorksAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws UnsealedWorksException, with the first unsealed services the caller may see, when any service of the
    /// period is unsealed, and otherwise StandInWorksPresentException when any service of the period lies on a stand-in.
    /// Neither can be acknowledged away.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureAllSealedAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);
}
