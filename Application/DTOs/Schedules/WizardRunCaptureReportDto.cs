// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Read-only view of what the captured wizard runs tell us so far. It answers the questions a future
/// learner would need: which engine and apply mode gets accepted, how much of a proposal survives
/// untouched, and whether starting from the previous period helps.
/// </summary>
/// <param name="TotalCaptures">Captured runs the report is built from.</param>
/// <param name="EngineStats">One entry per (engine, apply kind) combination that occurred.</param>
/// <param name="Training">Summary of the benchmark history.</param>
public sealed record WizardRunCaptureReportDto(
    int TotalCaptures,
    IReadOnlyList<WizardRunCaptureEngineStatsDto> EngineStats,
    WizardTrainingSummaryDto Training);
