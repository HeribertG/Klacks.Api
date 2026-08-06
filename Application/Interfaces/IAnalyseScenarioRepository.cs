// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository interface for AnalyseScenario CRUD and query operations.
/// </summary>
/// <param name="GetByGroupAsync">Returns scenarios, optionally filtered by group. Null returns all scenarios.</param>
/// <param name="GetByTokenAsync">Returns a scenario by its unique token</param>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IAnalyseScenarioRepository : IBaseRepository<AnalyseScenario>
{
    Task<List<AnalyseScenario>> GetByGroupAsync(Guid? groupId, CancellationToken ct = default);
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
