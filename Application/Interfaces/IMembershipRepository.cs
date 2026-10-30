// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IMembershipRepository : IBaseRepository<Membership>
{
    /// <summary>
    /// The memberships of one client, untracked and without soft-deleted rows.
    /// </summary>
    /// <param name="clientId">The client</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<List<Membership>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}
