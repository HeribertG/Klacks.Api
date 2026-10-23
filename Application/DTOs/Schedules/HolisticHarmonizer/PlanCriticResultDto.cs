// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <summary>
/// Vision-critic review attached to a Wizard 3 result. Informational only: the findings never changed the plan.
/// </summary>
/// <param name="Status">Skipped, Completed or Failed.</param>
/// <param name="ModelId">Model that reviewed the plan; null when skipped before a model was chosen.</param>
/// <param name="Findings">Findings whose every cell was checked against the real plan.</param>
/// <param name="DiscardedFindingCount">Findings dropped because a cited cell did not match the plan.</param>
/// <param name="Message">Reason for Skipped or Failed; null when Completed.</param>
public sealed record PlanCriticResultDto(
    string Status,
    string? ModelId,
    IReadOnlyList<PlanCriticFindingDto> Findings,
    int DiscardedFindingCount,
    string? Message);
