// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IMembershipWindowReader
{
    /// <summary>
    /// Membership window per client; a client without a membership row has no entry (unrestricted).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, MembershipWindow>> GetWindowsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken);
}