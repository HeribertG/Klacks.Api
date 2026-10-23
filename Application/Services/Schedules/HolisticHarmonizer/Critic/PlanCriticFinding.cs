// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// One pattern the vision critic sees in a finished plan, e.g. a night shift directly followed by an early shift.
/// </summary>
/// <param name="Pattern">Short snake_case label of the pattern, chosen by the critic (e.g. "night_then_early").</param>
/// <param name="Description">One or two sentences in the run language explaining why the pattern is a problem.</param>
/// <param name="Severity">How strongly the critic rates the finding.</param>
/// <param name="Cells">The cells that show the pattern; never empty for a verified finding.</param>
public sealed record PlanCriticFinding(
    string Pattern,
    string Description,
    PlanCriticSeverity Severity,
    IReadOnlyList<PlanCriticCell> Cells);
