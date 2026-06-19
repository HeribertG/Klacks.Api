// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Orchestrates a single Wizard-4 background optimisation pass over the current real/accepted plan:
/// builds the bitmap + objective contexts for the period, runs the anytime engine core, and — only
/// when it found a meaningful improvement — materialises the result as a new candidate AnalyseScenario
/// the operator can accept or reject. The in-memory bitmap captured at run start is the immutable
/// snapshot; concurrent edits to the real plan are caught by the pre-accept staleness check.
/// </summary>
public interface IWizard4Runner
{
    /// <summary>
    /// Runs one optimisation pass. Returns the created candidate scenario, or null when no improvement
    /// beyond the threshold was found (so no candidate is created).
    /// </summary>
    Task<AnalyseScenarioResource?> RunOnceAsync(
        Guid? groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        IReadOnlyList<Guid> agentIds,
        TimeSpan budget,
        CancellationToken ct);
}
