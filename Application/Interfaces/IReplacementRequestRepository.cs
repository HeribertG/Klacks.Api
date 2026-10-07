// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository of the replacement request book. Stage-only like the core schedule repositories: Add, the
/// tracked reads and every change to a loaded row are committed by the caller's IUnitOfWork. The one exception
/// is <see cref="DeleteReportedBeforeAsync"/>, a bulk retention delete that commits on its own and is only
/// called by the retention background service.
/// </summary>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IReplacementRequestRepository : IBaseRepository<ReplacementRequest>
{
    /// <summary>
    /// The live row for a natural key, including a row added in this unit of work but not yet saved, so two
    /// writes of the same slot within one request never collide on the unique index.
    /// </summary>
    /// <param name="analyseToken">Scenario token; null matches only real-plan rows</param>
    /// <param name="candidateClientId">Candidate employee</param>
    /// <param name="shiftId">Source shift of the slot</param>
    /// <param name="date">Date of the slot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ReplacementRequest?> FindLiveAsync(
        Guid? analyseToken, Guid candidateClientId, Guid shiftId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every live row of one scenario, tracked, including rows added in this unit of work - one query for a
    /// whole cover_absence run instead of one per slot.
    /// </summary>
    /// <param name="analyseToken">Scenario token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<ReplacementRequest>> ListLiveByTokenAsync(Guid analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// The live ManualReplacement row recorded for a WorkChange (tracked, staged rows included), or null.
    /// </summary>
    /// <param name="workChangeId">Id of the replacement WorkChange</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ReplacementRequest?> FindManualByWorkChangeIdAsync(Guid workChangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The report instant of the absence a scenario covers for one absent employee (earliest live row), or null.
    /// </summary>
    /// <param name="analyseToken">Scenario token</param>
    /// <param name="absentClientId">Absent employee</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<DateTime?> FindReportedAtAsync(Guid analyseToken, Guid absentClientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Live rows matching the filter, newest slot first, at most maxRows. Visibility is NOT applied here; the
    /// caller filters.
    /// </summary>
    /// <param name="filter">Optional absent employee, inclusive date range and scenario token</param>
    /// <param name="maxRows">Upper bound of returned rows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<ReplacementRequest>> ListAsync(
        ReplacementRequestFilter filter, int maxRows, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing bulk HARD delete of every row (live or soft-deleted) reported before the cutoff
    /// (retention: the owner decided that 24 months really means deleted).
    /// </summary>
    /// <param name="cutoffUtc">Rows with ReportedAtUtc before this instant are deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted rows</returns>
    Task<int> DeleteReportedBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
