// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Progress snapshot broadcast per generation.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="Generation">Current generation number (1-based)</param>
/// <param name="MaxGenerations">Configured upper bound</param>
/// <param name="BestHardViolations">Stage-0 value of the current best scenario</param>
/// <param name="BestStage1Completion">Stage-1 completion rate (0..1)</param>
/// <param name="BestStage2Score">Stage-2 weighted coverage (0..1)</param>
/// <param name="EarlyStopping">True if the loop is about to terminate</param>
public sealed record WizardJobProgressDto(
    Guid JobId,
    int Generation,
    int MaxGenerations,
    int BestHardViolations,
    double BestStage1Completion,
    double BestStage2Score,
    bool EarlyStopping);
