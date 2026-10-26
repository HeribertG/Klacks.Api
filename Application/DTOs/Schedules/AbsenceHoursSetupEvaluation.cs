// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Outcome of advising on an employee's absence-hours setup. Facts (the real calculation) are kept apart from the
/// findings and the recommendation, which are computed deterministically; nothing is changed.
/// </summary>
/// <param name="Calculation">The real calculation of the day, every figure with its source</param>
/// <param name="DesiredHoursPerDay">Hours per absence day the administrator wants, when given</param>
/// <param name="Findings">Deterministic findings, most important first</param>
/// <param name="Recommendation">Conservative summary naming the place to change</param>
public sealed record AbsenceHoursSetupEvaluation(
    AbsenceHoursCalculationView Calculation,
    decimal? DesiredHoursPerDay,
    IReadOnlyList<AbsenceHoursSetupFinding> Findings,
    string Recommendation);
