// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Materializes a set of agent-chosen placements into an isolated AnalyseScenario for supervised
/// approval. It does NOT find gaps (that is the wizard's job via start_autowizard) — it writes the
/// chosen placements, clones the real schedule under the token first, runs the pre-commit guardrail
/// so the written scenario stays collision-free, and skips-and-reports any blocking placement.
/// </summary>
public interface IProposePlanService
{
    Task<ProposePlanOutcome> ProposeAsync(
        Guid? groupId,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyList<PlacementInput> placements,
        CancellationToken cancellationToken = default);
}
