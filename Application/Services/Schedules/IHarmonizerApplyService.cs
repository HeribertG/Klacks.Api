// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Materialises a cached harmonizer result into Work entities. Always writes into a new
/// AnalyseScenario so the source schedule remains untouched and the user can compare or
/// roll back.
/// </summary>
public interface IHarmonizerApplyService
{
    Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct);
}
