// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Persisted record of a single wizard benchmark/training run.
 * Stores the effective TokenEvolutionConfig (as JSON) together with the measured metrics
 * so a training loop or dashboard can query the best historical configuration.
 * @param Source - "manual" when triggered by an API call, "background" when the hosted service produced it
 * @param ConfigJson - Serialized TokenEvolutionConfig actually used for this run
 * @param DurationMs - Wall-clock duration of the evolution loop
 * @param Stage0Violations - Hard-constraint violations (lower = better)
 * @param Stage1Completion - Completion ratio [0..1]
 * @param Stage2Score - Soft-constraint score
 * @param TokenCount - Non-locked tokens produced
 * @param AvailableShiftSlots - Total slots in the scenario
 * @param CoverageRatio - Tokens / slots, capped at 1.0
 * @param ClientDayDuplicates - (Client, Date) pairs with more than one assignment
 * @param AgentsCount - Number of agents in the benchmark scenario
 * @param ShiftsCount - Number of distinct shift definitions
 * @param PeriodDays - Period length in days
 */

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class WizardTrainingRun : BaseEntity
{
    public string Source { get; set; } = "manual";

    public string ConfigJson { get; set; } = "{}";

    public long DurationMs { get; set; }

    public int Stage0Violations { get; set; }

    public double Stage1Completion { get; set; }

    public double Stage2Score { get; set; }

    public int TokenCount { get; set; }

    public int AvailableShiftSlots { get; set; }

    public double CoverageRatio { get; set; }

    public int ClientDayDuplicates { get; set; }

    public int AgentsCount { get; set; }

    public int ShiftsCount { get; set; }

    public int PeriodDays { get; set; }
}
