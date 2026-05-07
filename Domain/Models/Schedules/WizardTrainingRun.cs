// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persisted record of a single wizard benchmark/training run.
/// Stores the effective TokenEvolutionConfig (as JSON) together with the measured metrics
/// so a training loop or dashboard can query the best historical configuration.
/// </summary>

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
