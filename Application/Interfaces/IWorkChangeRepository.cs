// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkChangeRepository : IBaseRepository<WorkChange>
{
    /// <summary>
    /// Loads a WorkChange (untracked) with its parent Work regardless of its scope: unlike Get, scenario rows
    /// (AnalyseToken set) are returned too. For write paths that must reach scenario changes.
    /// </summary>
    /// <param name="id">Id of the WorkChange</param>
    Task<WorkChange?> GetWithWorkInAnyScope(Guid id);

    /// <summary>
    /// The live replacement WorkChanges (untracked, with their parent Work) on the top-level works one employee holds on
    /// one workday in one scenario.
    /// </summary>
    /// <param name="analyseToken">Scenario token</param>
    /// <param name="clientId">Employee who owns the works</param>
    /// <param name="date">Workday</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<WorkChange>> ListScenarioReplacementsAsync(
        Guid analyseToken, Guid clientId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the employee still stands in on any work of the scenario.
    /// </summary>
    /// <param name="analyseToken">Scenario token</param>
    /// <param name="clientId">Replacing employee</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> IsReplacementInScenarioAsync(Guid analyseToken, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the employee already stands in on a work of the scenario at a time that overlaps the slot. Compares the
    /// effective replacement windows, including those of the neighbouring days that run past midnight.
    /// </summary>
    /// <param name="analyseToken">Scenario token</param>
    /// <param name="clientId">Replacing employee</param>
    /// <param name="date">Workday of the slot's work</param>
    /// <param name="workStart">Start of the slot's work; a slot starting before it lies after midnight, on the next day</param>
    /// <param name="start">Slot start</param>
    /// <param name="end">Slot end; at or before the start means the next day</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> HasOverlappingScenarioReplacementAsync(
        Guid analyseToken,
        Guid clientId,
        DateOnly date,
        TimeOnly workStart,
        TimeOnly start,
        TimeOnly end,
        CancellationToken cancellationToken = default);
}
