// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Progress snapshot broadcast per harmonizer generation.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="Generation">Current generation number (0 = initial population)</param>
/// <param name="MaxGenerations">Configured upper bound</param>
/// <param name="BestFitness">Best weighted-average row score so far in [0,1]</param>
/// <param name="EarlyStopping">True when stagnation triggered the loop to terminate</param>
public sealed record HarmonizerJobProgressDto(
    Guid JobId,
    int Generation,
    int MaxGenerations,
    double BestFitness,
    bool EarlyStopping);
