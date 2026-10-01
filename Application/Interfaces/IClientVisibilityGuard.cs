// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single place that decides whether the calling user may see, and therefore read or write, data owned by a
/// client. It follows the group visibility of the client list: admins and background callers without a user
/// are unrestricted, everybody else sees clients without an active group plus clients of their visible groups.
/// A hidden client must be answered exactly like a missing one.
/// </summary>
public interface IClientVisibilityGuard
{
    Task<bool> IsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when every id is visible; an empty collection is trivially visible. Answered with one query.
    /// </summary>
    /// <param name="clientIds">Owning client ids of the rows a command touches</param>
    Task<bool> AreAllVisibleAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps only the items whose owning client is visible, preserving their order. Answered with one query.
    /// </summary>
    /// <param name="items">Rows that each belong to one client</param>
    /// <param name="clientIdOf">Selects the owning client id of a row</param>
    Task<List<T>> FilterVisibleAsync<T>(
        IReadOnlyCollection<T> items, Func<T, Guid> clientIdOf, CancellationToken cancellationToken = default);
}
