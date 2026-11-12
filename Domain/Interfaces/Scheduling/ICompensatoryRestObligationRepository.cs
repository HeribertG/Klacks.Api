// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.CompensatoryRestObligation"/>
/// rows: the reconciler reads the active (non-deleted) obligations whose deadline window overlaps a
/// check range and upserts/soft-deletes them; the evaluator reads the open (unfulfilled) set for the
/// feed. Query results are tracked so the reconciler can mutate and soft-delete them in place.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface ICompensatoryRestObligationRepository
{
    Task<List<CompensatoryRestObligation>> GetActiveByClientInWindowAsync(
        Guid clientId,
        DateOnly windowStart,
        DateOnly windowEnd);

    Task<List<CompensatoryRestObligation>> GetOpenByClientAsync(Guid clientId);

    /// <summary>
    /// The open (unfulfilled) obligations of several clients in one read, per client ordered by trigger date like
    /// GetOpenByClientAsync. A client without an open obligation has no entry.
    /// </summary>
    /// <param name="clientIds">Clients to look up</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<Dictionary<Guid, List<CompensatoryRestObligation>>> GetOpenByClientsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default);

    void Add(CompensatoryRestObligation obligation);

    void Update(CompensatoryRestObligation obligation);

    void Delete(CompensatoryRestObligation obligation);
}
