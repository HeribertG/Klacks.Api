// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Refuses group membership additions of clients the caller cannot see, answering them exactly like a
/// missing client. Adding a hidden client to a visible group would make that client visible to the caller,
/// so only the added ids are checked; removing members of a visible group stays allowed.
/// </summary>

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Groups;

public static class GroupMemberVisibilityCheck
{
    public const string ClientNotFoundMessage = "Client with ID {0} not found";

    /// <param name="clientVisibilityGuard">Answers which of the added clients the caller may see</param>
    /// <param name="addedClientIds">Client ids that become new members of the group</param>
    /// <param name="cancellationToken">Cancels the visibility query</param>
    public static async Task EnsureAddedClientsVisibleAsync(
        IClientVisibilityGuard clientVisibilityGuard,
        IReadOnlyCollection<Guid> addedClientIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = addedClientIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return;
        }

        var visibleIds = (await clientVisibilityGuard.FilterVisibleAsync(distinctIds, id => id, cancellationToken))
            .ToHashSet();
        var hiddenIds = distinctIds.Where(id => !visibleIds.Contains(id)).ToList();
        if (hiddenIds.Count > 0)
        {
            throw new KeyNotFoundException(string.Format(ClientNotFoundMessage, hiddenIds[0]));
        }
    }
}
