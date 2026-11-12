// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the earliest date a client's employment record covers, used by PeriodCapEvaluator to clamp a
/// K6 rolling-average window so it never averages over weeks that predate the client's employment start
/// (see <see cref="Klacks.Api.Domain.Models.Associations.Membership"/>).
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IClientMembershipStartResolver
{
    Task<DateOnly?> GetValidFromAsync(Guid clientId);

    /// <summary>
    /// The employment start of several clients in one read. The result holds one entry per requested id; the value is
    /// null for a client without a membership row and for an id that matches no client.
    /// </summary>
    /// <param name="clientIds">Clients to look up</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<Dictionary<Guid, DateOnly?>> GetValidFromForClientsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default);
}
