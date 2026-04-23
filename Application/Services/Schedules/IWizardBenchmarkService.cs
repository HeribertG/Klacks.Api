// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Runs a synchronous wizard benchmark for training/measurement.
 * Same algorithm as the async Wizard/Start job, but returns all metrics directly
 * (no SignalR progress, no apply). The caller receives wall-clock time, final
 * fitness stages, token counts, coverage, and duplicate statistics in one call.
 * @param request - Period, agents, shifts, analyse-token, training overrides
 */

namespace Klacks.Api.Application.Services.Schedules;

public interface IWizardBenchmarkService
{
    Task<WizardBenchmarkResponse> RunAsync(WizardContextRequest request, CancellationToken ct);
}

/// <summary>
/// Result of a single benchmark run. Captures the effective config (what was actually used),
/// runtime metrics (wall-clock time), solution quality (fitness stages), and structural metrics
/// (token count, coverage, duplicates) so a training loop can compare configurations objectively.
/// </summary>
/// <param name="DurationMs">Wall-clock time of the evolution loop in milliseconds</param>
/// <param name="FinalHardViolations">Stage 0 - number of hard-constraint violations in the best solution (lower = better)</param>
/// <param name="FinalStage1Completion">Stage 1 - completion ratio [0..1] (higher = better)</param>
/// <param name="FinalStage2Score">Stage 2 - soft-constraint score (higher = better)</param>
/// <param name="TokenCount">Number of non-locked tokens in the best solution</param>
/// <param name="AvailableShiftSlots">Total shift slots the algorithm could have filled</param>
/// <param name="CoverageRatio">TokenCount / AvailableShiftSlots, capped at 1.0</param>
/// <param name="ClientDayDuplicates">Count of (ClientId, Date) pairs with more than one assignment (lower = better)</param>
/// <param name="DistinctAgents">How many agents received at least one token</param>
/// <param name="DistinctShifts">How many distinct shift definitions the tokens cover</param>
/// <param name="DistinctDates">How many calendar days the tokens cover</param>
/// <param name="EffectiveConfig">The TokenEvolutionConfig that was actually applied (baseline merged with overrides)</param>
public sealed record WizardBenchmarkResponse(
    long DurationMs,
    int FinalHardViolations,
    double FinalStage1Completion,
    double FinalStage2Score,
    int TokenCount,
    int AvailableShiftSlots,
    double CoverageRatio,
    int ClientDayDuplicates,
    int DistinctAgents,
    int DistinctShifts,
    int DistinctDates,
    WizardEffectiveConfigDto EffectiveConfig);

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
