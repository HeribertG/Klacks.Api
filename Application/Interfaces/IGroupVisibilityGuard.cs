// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single place that decides whether the calling user may write a group. Admins and background callers
/// without a user are unrestricted; everybody else may only write groups inside their visible group trees,
/// and a non-admin without any visibility row may write none (fail-closed). A hidden group must be answered
/// exactly like a missing one.
/// </summary>
public interface IGroupVisibilityGuard
{
    /// <summary>
    /// True when the caller is not limited by group visibility at all (admin, no calling user, or an
    /// installation without groups). Used by the bulk regrouping commands (partition by address, group by
    /// city name), which reorganise clients across the whole tenant.
    /// </summary>
    Task<bool> IsUnrestrictedAsync(CancellationToken cancellationToken = default);

    /// <param name="groupId">The group a command reads or writes</param>
    Task<bool> IsGroupVisibleAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when every id is visible; an empty collection is trivially visible.
    /// </summary>
    /// <param name="groupIds">The groups a command touches</param>
    Task<bool> AreAllGroupsVisibleAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default);
}
