// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Flat snapshot of the TokenEvolutionConfig that was applied to this run.
/// Mirrors TokenEvolutionConfig so training tools do not need to reference Klacks.ScheduleOptimizer directly.
/// </summary>
public sealed record WizardEffectiveConfigDto(
    int PopulationSize,
    int MaxGenerations,
    int TournamentK,
    double MutationRate,
    double CrossoverRate,
    int ElitismCount,
    double MutationWeightSwap,
    double MutationWeightSplit,
    double MutationWeightMerge,
    double MutationWeightReassign,
    double MutationWeightRepair,
    int EarlyStopNoImprovementGenerations,
    int RandomSeed);
