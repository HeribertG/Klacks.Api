// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Outcome of advising on an employee's (or the company's) target-hours setup. Facts are kept apart from the
/// findings and the recommendation, which are computed deterministically; nothing is changed.
/// </summary>
/// <param name="Calculation">The real resolution for the employee; null in company mode</param>
/// <param name="MonthlyTargetHoursOfYear">Monthly table of the year (month -> hours)</param>
/// <param name="WorkdaysPerMonth">Workdays of every month of the year</param>
/// <param name="DesiredWorkloadPercent">Workload share the administrator wants for the employee, when given</param>
/// <param name="Findings">Deterministic findings, most important first</param>
/// <param name="Recommendation">Conservative summary naming the way to use and the place to change</param>
public sealed record TargetHoursSetupEvaluation(
    TargetHoursCalculationView? Calculation,
    IReadOnlyDictionary<int, decimal> MonthlyTargetHoursOfYear,
    IReadOnlyDictionary<int, int> WorkdaysPerMonth,
    decimal? DesiredWorkloadPercent,
    IReadOnlyList<TargetHoursSetupFinding> Findings,
    string Recommendation);
