// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Optional per-request overrides for the TokenEvolution tuning parameters.
/// </summary>
/// <param name="PopulationSize">Overrides TokenEvolutionConfig.PopulationSize if set.</param>
/// <param name="MaxGenerations">Overrides TokenEvolutionConfig.MaxGenerations if set.</param>
/// <param name="TournamentK">Overrides TokenEvolutionConfig.TournamentK if set.</param>
/// <param name="MutationRate">Overrides TokenEvolutionConfig.MutationRate if set.</param>
/// <param name="CrossoverRate">Overrides TokenEvolutionConfig.CrossoverRate if set.</param>
/// <param name="ElitismCount">Overrides TokenEvolutionConfig.ElitismCount if set.</param>
/// <param name="MutationWeightSwap">Overrides TokenEvolutionConfig.MutationWeightSwap if set.</param>
/// <param name="MutationWeightSplit">Overrides TokenEvolutionConfig.MutationWeightSplit if set.</param>
/// <param name="MutationWeightMerge">Overrides TokenEvolutionConfig.MutationWeightMerge if set.</param>
/// <param name="MutationWeightReassign">Overrides TokenEvolutionConfig.MutationWeightReassign if set.</param>
/// <param name="MutationWeightRepair">Overrides TokenEvolutionConfig.MutationWeightRepair if set.</param>
/// <param name="EarlyStopNoImprovementGenerations">Overrides TokenEvolutionConfig.EarlyStopNoImprovementGenerations if set.</param>
/// <param name="RandomSeed">Overrides TokenEvolutionConfig.RandomSeed if set (null = random per run).</param>
/// <param name="InitAuctionRatio">Overrides TokenEvolutionConfig.InitAuctionRatio if set (0..1; clamped).</param>

using Klacks.ScheduleOptimizer.TokenEvolution;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WizardTrainingOverrides(
    int? PopulationSize = null,
    int? MaxGenerations = null,
    int? TournamentK = null,
    double? MutationRate = null,
    double? CrossoverRate = null,
    int? ElitismCount = null,
    double? MutationWeightSwap = null,
    double? MutationWeightSplit = null,
    double? MutationWeightMerge = null,
    double? MutationWeightReassign = null,
    double? MutationWeightRepair = null,
    int? EarlyStopNoImprovementGenerations = null,
    int? RandomSeed = null,
    double? InitAuctionRatio = null)
{
    public TokenEvolutionConfig Apply(TokenEvolutionConfig baseline) => baseline with
    {
        PopulationSize = PopulationSize ?? baseline.PopulationSize,
        MaxGenerations = MaxGenerations ?? baseline.MaxGenerations,
        TournamentK = TournamentK ?? baseline.TournamentK,
        MutationRate = MutationRate ?? baseline.MutationRate,
        CrossoverRate = CrossoverRate ?? baseline.CrossoverRate,
        ElitismCount = ElitismCount ?? baseline.ElitismCount,
        MutationWeightSwap = MutationWeightSwap ?? baseline.MutationWeightSwap,
        MutationWeightSplit = MutationWeightSplit ?? baseline.MutationWeightSplit,
        MutationWeightMerge = MutationWeightMerge ?? baseline.MutationWeightMerge,
        MutationWeightReassign = MutationWeightReassign ?? baseline.MutationWeightReassign,
        MutationWeightRepair = MutationWeightRepair ?? baseline.MutationWeightRepair,
        EarlyStopNoImprovementGenerations = EarlyStopNoImprovementGenerations ?? baseline.EarlyStopNoImprovementGenerations,
        RandomSeed = RandomSeed ?? baseline.RandomSeed,
        InitAuctionRatio = InitAuctionRatio is { } r ? Math.Clamp(r, 0d, 1d) : baseline.InitAuctionRatio,
    };
}
