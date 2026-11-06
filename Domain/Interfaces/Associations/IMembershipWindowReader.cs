// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Reads the membership windows the wizards close days by: the company membership (Membership) and the group period
/// (GroupItem validity) of a client in one group.
/// </summary>
public interface IMembershipWindowReader
{
    /// <summary>
    /// Membership window per client; a client without a membership row has no entry (unrestricted).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, MembershipWindow>> GetWindowsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Group period per client in exactly the given group (earliest ValidFrom, latest ValidUntil of the client's items in
    /// it, no subtree); a client without an item in that group has no entry (unrestricted). The same rule the schedule grid
    /// and the planning period guard apply.
    /// </summary>
    /// <param name="clientIds">Clients to look up</param>
    /// <param name="groupId">The group whose period applies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>> GetGroupPeriodRangesAsync(
        IReadOnlyCollection<Guid> clientIds,
        Guid groupId,
        CancellationToken cancellationToken);
}