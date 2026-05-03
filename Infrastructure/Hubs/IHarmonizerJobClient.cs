// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for harmonizer job progress streams.
/// </summary>
public interface IHarmonizerJobClient
{
    Task OnProgress(HarmonizerJobProgressDto progress);

    Task OnCompleted(HarmonizerJobResultDto result);

    Task OnCancelled();

    Task OnFailed(string reason);
}

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

/// <param name="AgentId">Owner of the row</param>
/// <param name="ScoreBefore">Harmony score before harmonisation</param>
/// <param name="ScoreAfter">Harmony score after harmonisation</param>
/// <param name="EmergencyUnlockTriggered">True if the row consumed its emergency unlock</param>
public sealed record HarmonizerRowResultDto(
    string AgentId,
    double ScoreBefore,
    double ScoreAfter,
    bool EmergencyUnlockTriggered);

/// <param name="JobId">Unique job identifier</param>
/// <param name="GlobalFitnessBefore">Weighted fitness of the source schedule</param>
/// <param name="GlobalFitnessAfter">Weighted fitness of the harmonised schedule</param>
/// <param name="GenerationsRun">Number of GA generations that actually executed</param>
/// <param name="RowResults">Per-agent before/after summary</param>
public sealed record HarmonizerJobResultDto(
    Guid JobId,
    double GlobalFitnessBefore,
    double GlobalFitnessAfter,
    int GenerationsRun,
    IReadOnlyList<HarmonizerRowResultDto> RowResults);
