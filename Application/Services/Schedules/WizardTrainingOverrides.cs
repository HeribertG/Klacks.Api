// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Optional per-request overrides for the TokenEvolution tuning parameters.
 * @param PopulationSize - Overrides TokenEvolutionConfig.PopulationSize if set
 * @param MaxGenerations - Overrides TokenEvolutionConfig.MaxGenerations if set
 * @param TournamentK - Overrides TokenEvolutionConfig.TournamentK if set
 * @param MutationRate - Overrides TokenEvolutionConfig.MutationRate if set
 * @param CrossoverRate - Overrides TokenEvolutionConfig.CrossoverRate if set
 * @param ElitismCount - Overrides TokenEvolutionConfig.ElitismCount if set
 * @param MutationWeightSwap - Overrides TokenEvolutionConfig.MutationWeightSwap if set
 * @param MutationWeightSplit - Overrides TokenEvolutionConfig.MutationWeightSplit if set
 * @param MutationWeightMerge - Overrides TokenEvolutionConfig.MutationWeightMerge if set
 * @param MutationWeightReassign - Overrides TokenEvolutionConfig.MutationWeightReassign if set
 * @param MutationWeightRepair - Overrides TokenEvolutionConfig.MutationWeightRepair if set
 * @param EarlyStopNoImprovementGenerations - Overrides TokenEvolutionConfig.EarlyStopNoImprovementGenerations if set
 * @param RandomSeed - Overrides TokenEvolutionConfig.RandomSeed if set (null = random per run)
 */

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
    int? RandomSeed = null)
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
    };
}
