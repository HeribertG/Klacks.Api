// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Applies a cached wizard scenario to the DB by materialising its non-locked tokens as Work entities.
/// </summary>
public interface IWizardApplyService
{
    /// <summary>
    /// Materialises the scenario identified by <paramref name="jobId"/>. Returns the list of
    /// newly created Work ids. Throws <see cref="InvalidOperationException"/> when no scenario is cached.
    /// </summary>
    Task<IReadOnlyList<Guid>> ApplyAsync(Guid jobId, CancellationToken ct);

    /// <summary>
    /// Creates a new AnalyseScenario with a full schedule clone, then writes the wizard's
    /// non-locked tokens into it. Returns the created scenario resource and the new Work ids.
    /// Throws <see cref="InvalidOperationException"/> when no scenario is cached for <paramref name="jobId"/>.
    /// </summary>
    Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(Guid jobId, Guid? groupId, CancellationToken ct);
}
