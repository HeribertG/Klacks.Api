// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Candidates;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Committee;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Committee.Agents;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Validation;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Measures one LLM model's Holistic Harmonizer proposal quality on the fixed in-memory eval
/// scenarios. Each scenario sends a small number of production-shaped ProposeAsync requests
/// (candidates, reject memory, PNG rendering) and pushes every returned batch through the real
/// acceptance pipeline (candidate filter, hard validator, committee, score-greedy). The
/// composite score (0..1) combines parse rate, batch acceptance rate and headroom-normalized
/// fitness improvement, and is persisted as one EvalRun under the harmonizer-v1 goldset with
/// regression against the latest run of the same model.
/// </summary>
/// <param name="proposalProvider">Real LLM-backed proposal source; mocked in unit tests.</param>
/// <param name="evalRunRepository">Persists the run and supplies the per-model baseline.</param>
/// <param name="logger">Diagnostic logger for per-scenario and aggregate outcomes.</param>
public sealed class HarmonizerEvalRunnerService : IHarmonizerEvalRunnerService
{
    private const int MaxStepsPerBatch = 3;
    private const string PromptLanguage = "en";
    private const decimal ParseWeight = 0.2m;
    private const decimal AcceptanceWeight = 0.4m;
    private const decimal ImprovementWeight = 0.4m;
    private const int CompositeScoreDecimals = 4;
    private const double ImprovementHeadroomEpsilon = 1e-9;

    private static readonly string[] IterationIntents =
    {
        HolisticIntent.ConsolidateBlock,
        HolisticIntent.RedistributeLoad,
    };

    private readonly IPlanProposalProvider _proposalProvider;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly ILogger<HarmonizerEvalRunnerService> _logger;

    public HarmonizerEvalRunnerService(
        IPlanProposalProvider proposalProvider,
        IEvalRunRepository evalRunRepository,
        ILogger<HarmonizerEvalRunnerService> logger)
    {
        _proposalProvider = proposalProvider;
        _evalRunRepository = evalRunRepository;
        _logger = logger;
    }

    public async Task<HarmonizerEvalRunResult> RunAsync(string modelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        var scenarios = HarmonizerEvalScenarioFactory.CreateAll();
        var stopwatch = Stopwatch.StartNew();

        var scenarioResults = new List<HarmonizerEvalScenarioResult>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scenarioResult = await RunScenarioAsync(scenario, modelId, cancellationToken);
            scenarioResults.Add(scenarioResult);
            _logger.LogInformation(
                "Harmonizer eval scenario {Scenario} model {Model}: fitness {Before:F4} -> {After:F4}, batches {Accepted}/{Proposed}, parsed {Parsed}/{Calls}, lastError={Error}",
                scenario.Name, modelId, scenarioResult.FitnessBefore, scenarioResult.FitnessAfter,
                scenarioResult.BatchesAccepted, scenarioResult.BatchesProposed,
                scenarioResult.LlmCallsParsed, scenarioResult.LlmCallsTotal,
                scenarioResult.LastError ?? "<none>");
        }

        stopwatch.Stop();

        var dimensions = Aggregate(scenarioResults);
        var composite = ComputeComposite(dimensions);

        var baseline = await _evalRunRepository.GetLatestAsync(HarmonizerEvalGoldset.Name, modelId, cancellationToken);
        decimal? regression = baseline == null ? null : composite - baseline.CompositeScore;

        var evalRun = new EvalRun
        {
            Id = Guid.NewGuid(),
            Goldset = HarmonizerEvalGoldset.Name,
            Provider = null,
            Model = modelId,
            CompositeScore = composite,
            DimensionsJson = JsonSerializer.Serialize(dimensions),
            RegressionVsBaseline = regression,
            ItemsTotal = dimensions.ScenariosTotal,
            ItemsPassed = dimensions.ScenariosWithAcceptedBatch,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            CreateTime = DateTime.UtcNow,
        };

        await _evalRunRepository.AddAsync(evalRun, cancellationToken);

        _logger.LogInformation(
            "Harmonizer eval run {Goldset} model {Model}: composite={Composite:F4}, parse={Parse:F2}, acceptance={Acceptance:F2}, improvement={Improvement:F2}, scenariosPassed={Passed}/{Total}, regression={Regression}",
            HarmonizerEvalGoldset.Name, modelId, composite,
            dimensions.ParseRate, dimensions.BatchAcceptanceRate, dimensions.NormalizedFitnessImprovement,
            dimensions.ScenariosWithAcceptedBatch, dimensions.ScenariosTotal, regression);

        return new HarmonizerEvalRunResult(evalRun, dimensions, scenarioResults);
    }

    private async Task<HarmonizerEvalScenarioResult> RunScenarioAsync(
        HarmonizerEvalScenario scenario,
        string modelId,
        CancellationToken cancellationToken)
    {
        var working = RowSorter.Sort(BitmapBuilder.Build(scenario.Input));

        var scorer = new HarmonyScorer();
        var fitness = new HarmonyFitnessEvaluator(scorer);
        var validator = new PlanMutationValidator(new DomainAwareReplaceValidator(
            scenario.Input.Availability,
            scenario.Input.BoundaryAssignments,
            scenario.Input.IneligibleAssignments));
        var committee = new ConstraintAgentCommittee(new IConstraintAgent[]
        {
            new HoursConstraintAgent(),
            new PauseConstraintAgent(scenario.Input.BoundaryAssignments),
            new ConsecutiveConstraintAgent(scenario.Input.BoundaryAssignments),
            new RotationConstraintAgent(),
            new PreferenceConstraintAgent(),
        });
        var batchEvaluator = new BatchEvaluator(validator, fitness, committee);
        var candidatePool = new MoveCandidatePool(
            validator,
            new IMoveCandidateGenerator[]
            {
                new ConsolidateBlockCandidateGenerator(),
                new EnlargePauseCandidateGenerator(),
                new RedistributeLoadCandidateGenerator(),
            });
        var rejectMemory = new RejectMemory();
        var pngRenderer = new HarmonyBitmapPngRenderer();
        var agentSummary = HolisticHarmonizerEngine.BuildAgentSummary(working);

        var fitnessBefore = fitness.Evaluate(working).Fitness;

        var llmCallsTotal = 0;
        var llmCallsParsed = 0;
        var batchesProposed = 0;
        var batchesAccepted = 0;
        string? lastError = null;

        for (var iter = 0; iter < IterationIntents.Length; iter++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var intent = IterationIntents[iter];
            var candidates = candidatePool.Generate(working, intent);
            var request = new PlanProposalRequest(
                ModelId: modelId,
                PlanText: HarmonyBitmapTextRenderer.Render(working),
                AgentSummary: agentSummary,
                FragmentationSummary: FragmentationAnalyzer.Render(working),
                MaxStepsPerBatch: MaxStepsPerBatch,
                Language: PromptLanguage,
                IterationIndex: iter,
                PriorRejections: rejectMemory.Entries.ToArray(),
                PlanPng: pngRenderer.Render(working),
                FocusedIntent: intent,
                CandidateMoves: candidates);

            llmCallsTotal++;
            var response = await _proposalProvider.ProposeAsync(request, cancellationToken);
            if (response.ParsingError is not null)
            {
                lastError = response.ParsingError;
                continue;
            }

            llmCallsParsed++;
            foreach (var rawBatch in response.Batches)
            {
                batchesProposed++;
                var batch = CandidateStepFilter.FilterToCandidates(rawBatch, candidates);
                if (batch.Steps.Count == 0)
                {
                    continue;
                }

                var evaluation = batchEvaluator.Evaluate(working, batch);
                if (evaluation.Result is BatchAcceptance.Accepted or BatchAcceptance.PartiallyAccepted)
                {
                    batchesAccepted++;
                }
                else
                {
                    rejectMemory.Note(evaluation);
                }
            }
        }

        var fitnessAfter = fitness.Evaluate(working).Fitness;

        return new HarmonizerEvalScenarioResult(
            Name: scenario.Name,
            FitnessBefore: fitnessBefore,
            FitnessAfter: fitnessAfter,
            LlmCallsTotal: llmCallsTotal,
            LlmCallsParsed: llmCallsParsed,
            BatchesProposed: batchesProposed,
            BatchesAccepted: batchesAccepted,
            LastError: lastError);
    }

    private static HarmonizerEvalDimensions Aggregate(IReadOnlyList<HarmonizerEvalScenarioResult> scenarios)
    {
        var callsTotal = scenarios.Sum(s => s.LlmCallsTotal);
        var callsParsed = scenarios.Sum(s => s.LlmCallsParsed);
        var proposed = scenarios.Sum(s => s.BatchesProposed);
        var accepted = scenarios.Sum(s => s.BatchesAccepted);

        var parseRate = callsTotal == 0 ? 0m : (decimal)callsParsed / callsTotal;
        var acceptanceRate = proposed == 0 ? 0m : (decimal)accepted / proposed;
        var improvement = scenarios.Count == 0
            ? 0m
            : (decimal)scenarios.Average(NormalizedImprovement);

        return new HarmonizerEvalDimensions(
            ParseRate: Math.Round(parseRate, CompositeScoreDecimals),
            BatchAcceptanceRate: Math.Round(acceptanceRate, CompositeScoreDecimals),
            NormalizedFitnessImprovement: Math.Round(improvement, CompositeScoreDecimals),
            LlmCallsTotal: callsTotal,
            LlmCallsParsed: callsParsed,
            BatchesProposed: proposed,
            BatchesAccepted: accepted,
            ScenariosTotal: scenarios.Count,
            ScenariosWithAcceptedBatch: scenarios.Count(s => s.BatchesAccepted > 0));
    }

    private static double NormalizedImprovement(HarmonizerEvalScenarioResult scenario)
    {
        var headroom = 1.0 - scenario.FitnessBefore;
        if (headroom <= ImprovementHeadroomEpsilon)
        {
            return 0.0;
        }

        var gain = (scenario.FitnessAfter - scenario.FitnessBefore) / headroom;
        return Math.Clamp(gain, 0.0, 1.0);
    }

    private static decimal ComputeComposite(HarmonizerEvalDimensions dimensions) =>
        Math.Round(
            ParseWeight * dimensions.ParseRate
            + AcceptanceWeight * dimensions.BatchAcceptanceRate
            + ImprovementWeight * dimensions.NormalizedFitnessImprovement,
            CompositeScoreDecimals);
}
