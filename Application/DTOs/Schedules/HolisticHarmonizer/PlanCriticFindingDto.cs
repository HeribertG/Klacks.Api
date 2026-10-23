// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="Pattern">Short snake_case label chosen by the critic, e.g. "night_then_early".</param>
/// <param name="Description">Explanation in the run language; refers to rows as r00, r01, ...</param>
/// <param name="Severity">Low, Medium or High.</param>
/// <param name="Cells">Verified cells that show the pattern.</param>
public sealed record PlanCriticFindingDto(
    string Pattern,
    string Description,
    string Severity,
    IReadOnlyList<PlanCriticCellDto> Cells);
