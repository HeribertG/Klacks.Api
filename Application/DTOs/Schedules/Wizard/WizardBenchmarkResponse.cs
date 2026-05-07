// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Result of a single benchmark run. Captures the effective config (what was actually used),
/// runtime metrics (wall-clock time), solution quality (fitness stages), and structural metrics
/// (token count, coverage, duplicates) so a training loop can compare configurations objectively.
/// </summary>
/// <param name="DurationMs">Wall-clock time of the evolution loop in milliseconds.</param>
/// <param name="FinalHardViolations">Stage 0 - number of hard-constraint violations in the best solution (lower = better).</param>
/// <param name="FinalStage1Completion">Stage 1 - completion ratio [0..1] (higher = better).</param>
/// <param name="FinalStage2Score">Stage 2 - soft-constraint score (higher = better).</param>
/// <param name="TokenCount">Number of non-locked tokens in the best solution.</param>
/// <param name="AvailableShiftSlots">Total shift slots the algorithm could have filled.</param>
/// <param name="CoverageRatio">TokenCount / AvailableShiftSlots, capped at 1.0 (legacy metric; does not account for overstaffing).</param>
/// <param name="ClientDayDuplicates">Count of (ClientId, Date) pairs with more than one assignment (lower = better).</param>
/// <param name="DistinctAgents">How many agents received at least one token.</param>
/// <param name="DistinctShifts">How many distinct shift definitions the tokens cover.</param>
/// <param name="DistinctDates">How many calendar days the tokens cover.</param>
/// <param name="CoveredShiftSlots">Number of shift slots (ShiftId, Date) that received at least one assignment (capped per slot by its capacity).</param>
/// <param name="OverstaffingCount">Sum of surplus assignments over slot capacity across all slots (tokens assigned beyond what the slot needs).</param>
/// <param name="UndersupplyCount">Sum of missing assignments across all slots (slots still needing agents).</param>
/// <param name="ShiftCoverageRatio">CoveredShiftSlots / AvailableShiftSlots — real percentage of required shift slots that are filled.</param>
/// <param name="AgentsInContext">Number of agents that actually reached the wizard context (may be less than requested if contract data is missing).</param>
/// <param name="AgentsShiftCapable">Subset of AgentsInContext with PerformsShiftWork=true (only these can cover Spät/Nacht slots).</param>
/// <param name="SlotsFillableByData">Number of slots that at least one agent in the context could theoretically fill, considering the shift-work flag.</param>
/// <param name="TheoreticalMaxCoverage">SlotsFillableByData / AvailableShiftSlots — upper bound on ShiftCoverageRatio given the current agent pool.</param>
/// <param name="EffectiveConfig">The TokenEvolutionConfig that was actually applied (baseline merged with overrides).</param>
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
    int CoveredShiftSlots,
    int OverstaffingCount,
    int UndersupplyCount,
    double ShiftCoverageRatio,
    int AgentsInContext,
    int AgentsShiftCapable,
    int SlotsFillableByData,
    double TheoreticalMaxCoverage,
    WizardEffectiveConfigDto EffectiveConfig);
