// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <summary>
/// Materializes a set of chosen placements into an isolated AnalyseScenario for supervised approval.
/// Does NOT find gaps (that is the wizard via start_autowizard) — it writes the chosen placements,
/// clones the real schedule under the token first, runs the pre-commit guardrail so the written
/// scenario stays collision-free, and reports blocking placements instead of committing them.
/// </summary>
/// <param name="GroupId">Group / planning blade the scenario belongs to (null = unfiltered)</param>
/// <param name="FromDate">Scenario period start</param>
/// <param name="UntilDate">Scenario period end (inclusive)</param>
/// <param name="Placements">Placements to propose</param>
/// <param name="OverrideBlock">
/// K1 supervisor override: when true and the caller holds the Admin or Authorised role, a placement
/// blocked only by Block-mode compliance enforcement (never a structural error such as a collision or
/// a missing mandatory qualification) is written anyway and logged as an override.
/// </param>
public record ProposePlanCommand(
    Guid? GroupId,
    DateOnly FromDate,
    DateOnly UntilDate,
    IReadOnlyList<PlacementInput> Placements,
    bool OverrideBlock = false) : IRequest<ProposePlanOutcome>;
