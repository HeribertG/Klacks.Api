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
}
