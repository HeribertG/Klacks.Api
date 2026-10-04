// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Search;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Default stage-3 engine without any LLM: loads the same context as the LLM engine and runs the
/// <see cref="DeterministicHarmonyOptimizer"/> (best-improvement local search over the candidate pool with pairs,
/// tabu list and fixed seed) through the same acceptance stack. Works offline; same input and seed give the same
/// plan unless the wall-clock safety budget stops the search.
/// </summary>
/// <param name="contextBuilder">Reuses the Wizard 2 context builder to read the schedule.</param>
/// <param name="logger">Logs one line per applied batch (coordinates, enums, scores only) and a run summary.</param>
public sealed class HolisticHarmonizerDeterministicEngine
{
    /// <summary>Label reported in place of an LLM model id so run results and logs show which mode produced them.</summary>
    public const string EngineLabel = "deterministic-local-search";

    private const string EmptyLogValue = "-";
    private const string SwapListSeparator = ",";
    private const string SwapLogFormat = "r{0}d{1}<->r{2}d{3}";

    private readonly IHarmonizerContextBuilder _contextBuilder;
    private readonly ILogger<HolisticHarmonizerDeterministicEngine> _logger;

    public HolisticHarmonizerDeterministicEngine(
        IHarmonizerContextBuilder contextBuilder,
        ILogger<HolisticHarmonizerDeterministicEngine> logger)
    {
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<HolisticHarmonizerRunResult> RunAsync(
        HolisticHarmonizerRunInput input,
        IProgress<HolisticHarmonizerProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var context = await _contextBuilder.BuildContextAsync(
            new HarmonizerContextRequest(
                input.PeriodFrom,
                input.PeriodUntil,
                input.AgentIds,
                input.AnalyseToken,
                input.ContextDaysBefore,
                input.ContextDaysAfter),
            cancellationToken);

        var sorted = RowSorter.Sort(BitmapBuilder.Build(context));
        var original = BitmapCloner.Clone(sorted);
        var working = BitmapCloner.Clone(sorted);

        var fitness = new MemoizedHarmonyFitnessEvaluator(new HarmonyScorer());
        var components = HolisticHarmonizerComponents.Build(context, fitness, HolisticHarmonizerComponents.UntrimmedPool);
        var options = DeterministicSearchOptions.Default;
        var optimizer = new DeterministicHarmonyOptimizer(components, options);

        var result = optimizer.Run(working, progress, cancellationToken);

        for (var i = 0; i < result.AppliedBatches.Count; i++)
        {
            LogAppliedBatch(i, result.AppliedBatches[i]);
        }

        _logger.LogInformation(
            "Holistic Harmonizer (deterministic) finished: rows={Rows} days={Days} applied={Applied} iterations={Iterations} evaluations={Evaluations} maxCandidates={MaxCandidates} stop={Stop} fitness {Before:F4} -> {After:F4} elapsed={Ms}ms pairCap={PairCap} seed={Seed} restarts={Restarts} bestRestart={BestRestart} totalBudgetHit={TotalBudgetHit} memoHits={Hits} memoMisses={Misses}",
            working.RowCount,
            working.DayCount,
            result.AppliedBatches.Count,
            result.IterationsRun,
            result.Evaluations,
            result.MaxCandidatesPerIteration,
            result.StopReason,
            result.FitnessBefore,
            result.FitnessAfter,
            result.ElapsedMs,
            options.PairPoolCap,
            options.Seed,
            result.RestartsRun,
            result.BestRestart,
            result.TotalEvaluationBudgetHit,
            fitness.Hits,
            fitness.Misses);

        if (components.Rules is { } rules)
        {
            var rulesBefore = rules.Evaluate(original);
            var rulesAfter = rules.Evaluate(working);
            _logger.LogInformation(
                "Holistic Harmonizer (deterministic) planning rules: rules={Rules} hardFindings {HardBefore} -> {HardAfter} softPenalty {SoftBefore:F3} -> {SoftAfter:F3}",
                rules.Rules.Count,
                rulesBefore.HardCount,
                rulesAfter.HardCount,
                rulesBefore.SoftPenalty,
                rulesAfter.SoftPenalty);
        }

        if (result.StopReason == DeterministicSearchStopReason.WallClockBudget)
        {
            _logger.LogWarning(
                "Holistic Harmonizer (deterministic) hit the wall-clock budget of {Budget}s before its evaluation budget; this run is not reproducible",
                options.WallClockBudget.TotalSeconds);
        }

        return new HolisticHarmonizerRunResult(
            OriginalBitmap: original,
            FinalBitmap: working,
            Iterations: result.AppliedBatches,
            FitnessBefore: result.FitnessBefore,
            FitnessAfter: result.FitnessAfter,
            LlmModelId: EngineLabel,
            LlmParsingError: null,
            LlmRawResponsePreview: null)
        {
            InvalidPlanningRuleIds = context.Rules?.InvalidHardRuleIds ?? [],
        };
    }

    private void LogAppliedBatch(int iteration, BatchEvaluation evaluation)
        => _logger.LogInformation(
            "Holistic Harmonizer (deterministic) iter={Iter} batch={BatchId} intent={Intent} result={Result} applied={Applied} score {Before:F4} -> {After:F4}",
            iteration,
            evaluation.BatchId,
            evaluation.Intent,
            evaluation.Result,
            evaluation.AppliedSteps.Count == 0
                ? EmptyLogValue
                : string.Join(SwapListSeparator, evaluation.AppliedSteps.Select(s =>
                    string.Format(CultureInfo.InvariantCulture, SwapLogFormat, s.RowA, s.DayA, s.RowB, s.DayB))),
            evaluation.ScoreBefore,
            evaluation.ScoreAfter);
}
