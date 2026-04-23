// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Default implementation of IWizardBenchmarkService.
 * Composes the same context-builder + TokenEvolutionLoop as WizardJobRunner, but runs synchronously
 * and returns a rich metrics snapshot instead of streaming SignalR progress events.
 * @param scopeFactory - Creates an isolated DI scope per benchmark (fresh DbContext, repositories)
 */

using System.Diagnostics;
using Klacks.Api.Application.Services.Schedules;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.TokenEvolution;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class WizardBenchmarkService : IWizardBenchmarkService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WizardBenchmarkService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<WizardBenchmarkResponse> RunAsync(WizardContextRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IWizardContextBuilder>();
        var wizardContext = await builder.BuildContextAsync(request, ct);

        var baseline = new TokenEvolutionConfig
        {
            RandomSeed = Guid.NewGuid().GetHashCode(),
        };
        var config = request.TrainingOverrides?.Apply(baseline) ?? baseline;

        var loop = TokenEvolutionLoop.Create();

        var stopwatch = Stopwatch.StartNew();
        var best = loop.Run(wizardContext, config, progress: null, ct);
        stopwatch.Stop();

        return BuildResponse(best, wizardContext, config, stopwatch.ElapsedMilliseconds);
    }

    private static WizardBenchmarkResponse BuildResponse(
        CoreScenario best,
        CoreWizardContext context,
        TokenEvolutionConfig config,
        long durationMs)
    {
        var nonLocked = best.Tokens.Where(t => !t.IsLocked).ToList();

        var availableSlots = context.Shifts.Count;
        var coverage = availableSlots == 0
            ? 0d
            : Math.Min(1d, (double)nonLocked.Count / availableSlots);

        var clientDayDuplicates = nonLocked
            .GroupBy(t => (t.AgentId, t.Date))
            .Count(g => g.Count() > 1);

        return new WizardBenchmarkResponse(
            DurationMs: durationMs,
            FinalHardViolations: best.FitnessStage0,
            FinalStage1Completion: best.FitnessStage1,
            FinalStage2Score: best.FitnessStage2,
            TokenCount: nonLocked.Count,
            AvailableShiftSlots: availableSlots,
            CoverageRatio: coverage,
            ClientDayDuplicates: clientDayDuplicates,
            DistinctAgents: nonLocked.Select(t => t.AgentId).Distinct().Count(),
            DistinctShifts: nonLocked.Select(t => t.ShiftRefId).Distinct().Count(),
            DistinctDates: nonLocked.Select(t => t.Date).Distinct().Count(),
            EffectiveConfig: new WizardEffectiveConfigDto(
                PopulationSize: config.PopulationSize,
                MaxGenerations: config.MaxGenerations,
                TournamentK: config.TournamentK,
                MutationRate: config.MutationRate,
                CrossoverRate: config.CrossoverRate,
                ElitismCount: config.ElitismCount,
                MutationWeightSwap: config.MutationWeightSwap,
                MutationWeightSplit: config.MutationWeightSplit,
                MutationWeightMerge: config.MutationWeightMerge,
                MutationWeightReassign: config.MutationWeightReassign,
                MutationWeightRepair: config.MutationWeightRepair,
                EarlyStopNoImprovementGenerations: config.EarlyStopNoImprovementGenerations,
                RandomSeed: config.RandomSeed));
    }
}
