// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Condensed view of the benchmark history: how many runs were recorded recently and what the best
/// feasible configuration achieved. Only a run without hard violations qualifies as best - a faster
/// run that breaks the rules is not an improvement.
/// </summary>
/// <param name="RecentCount">Number of recent training runs the report looked at.</param>
/// <param name="BestConfigJson">Serialised configuration of the best feasible run; null when none exists.</param>
/// <param name="BestStage2Score">Coverage score of the best feasible run.</param>
/// <param name="BestDurationMs">Wall-clock time of the best feasible run.</param>
/// <param name="BestStage0Violations">Hard violations of the best feasible run; zero by definition.</param>
public sealed record WizardTrainingSummaryDto(
    int RecentCount,
    string? BestConfigJson,
    double? BestStage2Score,
    long? BestDurationMs,
    int? BestStage0Violations);
