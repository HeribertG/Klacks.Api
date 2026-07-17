// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    void Add(CompensatoryRestObligation obligation);

    void Update(CompensatoryRestObligation obligation);

    void Delete(CompensatoryRestObligation obligation);
}
