// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.PlanningConstraint"/> rows. Writes
/// are staged only (Add/Remove) and committed by IUnitOfWork; the one exception is the expiry sweep, a single
/// conditional bulk update that is safe to run on several instances at once.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IPlanningConstraintRepository
{
    Task<PlanningConstraint?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PlanningConstraint>> ListAsync(RuleApprovalStatus? status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approved, not deleted constraints whose validity overlaps [from, until]: the real rows (AnalyseToken
    /// null) plus, for a scenario (<paramref name="analyseToken"/> set), that scenario's own rows.
    /// </summary>
    Task<List<PlanningConstraint>> GetApprovedForPeriodAsync(
        DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken cancellationToken = default);

    void Add(PlanningConstraint constraint);

    void Remove(PlanningConstraint constraint);

    /// <summary>
    /// Moves every Proposed row created before <paramref name="createdBeforeUtc"/> to Rejected in one
    /// conditional update and returns the number of rows changed.
    /// </summary>
    Task<int> ExpireProposedAsync(DateTime createdBeforeUtc, DateTime nowUtc, CancellationToken cancellationToken = default);
}
