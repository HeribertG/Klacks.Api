// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository interface for AnalyseScenario CRUD and query operations.
/// </summary>
/// <param name="GetByGroupAsync">Returns the scenarios of exactly this group; null returns only the group-less scenarios.</param>
/// <param name="GetByTokenAsync">Returns a scenario by its unique token</param>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IAnalyseScenarioRepository : IBaseRepository<AnalyseScenario>
{
    Task<List<AnalyseScenario>> GetByGroupAsync(Guid? groupId, CancellationToken ct = default);

    /// <summary>
    /// Scenarios for the assistant's scenario list, filtered in the database: not deleted, optionally one group
    /// only, optionally only the open (Active) ones, and - for a caller with a restricted group scope - only the
    /// scenarios of groups under the visible roots (group-less scenarios span all groups and are left out).
    /// Newest first, each with its group loaded.
    /// </summary>
    /// <param name="groupId">Only this group's scenarios; null for every group.</param>
    /// <param name="onlyOpen">True to return only Active scenarios.</param>
    /// <param name="visibleRootIds">Root group ids the caller may see; null when the caller is unrestricted.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<AnalyseScenario>> ListVisibleAsync(
        Guid? groupId, bool onlyOpen, IReadOnlyCollection<Guid>? visibleRootIds, CancellationToken ct = default);

    /// <summary>
    /// Active, not deleted scenarios created inside a window - the proposals that wait for an answer. Filtered
    /// in the database, each with its group loaded; scenarios without a create time are left out.
    /// </summary>
    /// <param name="createdAfterUtc">Oldest create time still reported (exclusive lower bound).</param>
    /// <param name="createdBeforeUtc">Newest create time reported (inclusive upper bound).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<AnalyseScenario>> GetActiveCreatedBetweenAsync(
        DateTime createdAfterUtc, DateTime createdBeforeUtc, CancellationToken ct = default);
    Task<AnalyseScenario?> GetByTokenAsync(Guid token, CancellationToken ct = default);

    /// <summary>
    /// The newest still-active scenario one author created for exactly this selection, or null.
    /// Used to replace a background candidate rather than stack a second one next to it.
    /// </summary>
    /// <param name="createdByUser">Author to match.</param>
    /// <param name="groupId">Group of the selection; null matches the group-less scenarios.</param>
    /// <param name="fromDate">Start of the scenario range.</param>
    /// <param name="untilDate">End of the scenario range.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AnalyseScenario?> GetActiveCandidateAsync(
        string createdByUser, Guid? groupId, DateOnly fromDate, DateOnly untilDate, CancellationToken ct = default);

    /// <summary>
    /// Still-active scenarios of one author created before a cutoff - the candidates that timed out.
    /// </summary>
    /// <param name="createdByUser">Author to match.</param>
    /// <param name="createdBeforeUtc">Everything created before this instant counts as stale.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<AnalyseScenario>> GetStaleCandidatesAsync(
        string createdByUser, DateTime createdBeforeUtc, CancellationToken ct = default);
}
