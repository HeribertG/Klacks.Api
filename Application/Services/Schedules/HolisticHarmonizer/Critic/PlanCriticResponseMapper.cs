// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Maps a <see cref="PlanCriticOutcome"/> to the wire DTO carried by the Wizard 3 run response.
/// </summary>
public static class PlanCriticResponseMapper
{
    public static PlanCriticResultDto ToDto(PlanCriticOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return new PlanCriticResultDto(
            outcome.Status.ToString(),
            outcome.ModelId,
            outcome.Findings
                .Select(f => new PlanCriticFindingDto(
                    f.Pattern,
                    f.Description,
                    f.Severity.ToString(),
                    f.Cells.Select(c => new PlanCriticCellDto(c.Row, c.Day, c.Symbol)).ToList()))
                .ToList(),
            outcome.DiscardedFindingCount,
            outcome.Message);
    }
}
