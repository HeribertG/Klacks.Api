// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Materialises a cached Holistic Harmonizer result into a new AnalyseScenario. Reuses the harmonizer
/// pipeline (same shared <c>HarmonizerResultCache</c>, same Bitmap → Work conversion) but
/// stamps the scenario with the "LLM" name prefix and the inherited <c>RunGroupId</c> so
/// Wizard 1/2/3 outputs from the same test run are correlatable in the scenario list.
/// </summary>
public interface IHolisticHarmonizerApplyService
{
    /// <param name="namePrefixOverride">Overrides the default "LLM" name prefix; null keeps the default.</param>
    Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct,
        string? namePrefixOverride = null);
}
