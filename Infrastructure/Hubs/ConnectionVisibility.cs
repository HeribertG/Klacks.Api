// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the user behind a schedule connection may see, resolved once when the connection joins a schedule range or
/// changes its group filter, and cached on the connection state so a notification never resolves group visibility per
/// message. Unrestricted for admins and users without visibility restrictions; otherwise the clients and groups of the
/// user's visible group trees. A connection whose visibility was never resolved sees nothing (fail-closed).
/// </summary>
/// <param name="IsUnrestricted">True when the user is not limited by group visibility</param>
/// <param name="ClientIds">Clients of the user's visible groups; ignored when unrestricted</param>
/// <param name="GroupIds">The user's visible groups including subgroups; ignored when unrestricted</param>

namespace Klacks.Api.Infrastructure.Hubs;

public sealed record ConnectionVisibility(bool IsUnrestricted, IReadOnlySet<Guid> ClientIds, IReadOnlySet<Guid> GroupIds)
{
    public static ConnectionVisibility Unrestricted { get; } = new(true, new HashSet<Guid>(), new HashSet<Guid>());

    public static ConnectionVisibility None { get; } = new(false, new HashSet<Guid>(), new HashSet<Guid>());

    public static ConnectionVisibility Restricted(IEnumerable<Guid> clientIds, IEnumerable<Guid> groupIds)
        => new(false, clientIds.ToHashSet(), groupIds.ToHashSet());

    public bool AllowsClient(Guid clientId) => IsUnrestricted || ClientIds.Contains(clientId);

    public bool AllowsGroup(Guid groupId) => IsUnrestricted || GroupIds.Contains(groupId);
}
