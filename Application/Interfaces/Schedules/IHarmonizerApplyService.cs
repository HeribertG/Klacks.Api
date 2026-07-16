// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Materialises a cached harmonizer result into Work entities. Always writes into a new
/// AnalyseScenario so the source schedule remains untouched and the user can compare or
/// roll back.
/// </summary>
public interface IHarmonizerApplyService
{
    /// <param name="namePrefixOverride">Overrides the default "Harmonisiert" / "LLM" name prefix; null keeps the default.</param>
    /// <param name="captureRun">When true, writes a WizardRunCapture row for the (deferred) preference-learner.
    /// Wizard 4 sets this false because its runner writes its own composite capture after materialising through this path.</param>
    Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct,
        string? namePrefixOverride = null,
        bool captureRun = true);
}
