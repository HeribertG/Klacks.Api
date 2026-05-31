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
public record ProposePlanCommand(
    Guid? GroupId,
    DateOnly FromDate,
    DateOnly UntilDate,
    IReadOnlyList<PlacementInput> Placements) : IRequest<ProposePlanOutcome>;
