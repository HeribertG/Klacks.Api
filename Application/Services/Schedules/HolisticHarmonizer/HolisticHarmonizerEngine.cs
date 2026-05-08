// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Committee;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Committee.Agents;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Validation;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Holistic Holistic Harmonizer engine. Runs an inner LLM-ALNS loop: each iteration renders the current
/// bitmap, asks the LLM for one or more <see cref="MutationBatch"/> proposals (each tagged with
/// an intent label), evaluates them as atomic transformations with Score-Greedy and prefix
/// acceptance, and feeds rejects back to the LLM via a small reject memory. The inner loop
/// stops on plateau, time budget, or iteration cap.
/// </summary>
/// <param name="contextBuilder">Reuses the Wizard 2 context builder to read the schedule.</param>
/// <param name="proposalProvider">LLM-backed batch proposal source.</param>
/// <param name="logger">Diagnostic logger for proposal counts, score trajectory, failures.</param>
public sealed class HolisticHarmonizerEngine
{
    private const int MaxInnerIterations = 10;
    private const int PlateauStopThreshold = 3;
    private const int RawResponsePreviewLength = 600;
    private static readonly TimeSpan InnerLoopTimeBudget = TimeSpan.FromSeconds(90);

    private readonly IHarmonizerContextBuilder _contextBuilder;
    private readonly IPlanProposalProvider _proposalProvider;
    private readonly ILogger<HolisticHarmonizerEngine> _logger;

    public HolisticHarmonizerEngine(
        IHarmonizerContextBuilder contextBuilder,
        IPlanProposalProvider proposalProvider,
        ILogger<HolisticHarmonizerEngine> logger)
    {
        _contextBuilder = contextBuilder;
        _proposalProvider = proposalProvider;
        _logger = logger;
    }

    public Task<HolisticHarmonizerRunResult> RunAsync(HolisticHarmonizerEngineRequest request, CancellationToken cancellationToken)
        => RunAsync(request, progress: null, cancellationToken);

    public async Task<HolisticHarmonizerRunResult> RunAsync(
        HolisticHarmonizerEngineRequest request,
        IProgress<HolisticHarmonizerProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var ping = await _proposalProvider.PingAsync(request.LlmModelId, cancellationToken);
        _logger.LogInformation(
            "Holistic Harmonizer ping: model={Model} healthy={Healthy} latency={LatencyMs}ms error={Error}",
            request.LlmModelId, ping.IsHealthy, ping.LatencyMs, ping.Error ?? "<none>");

        var contextRequest = new HarmonizerContextRequest(
            request.PeriodFrom,
            request.PeriodUntil,
            request.AgentIds,
            request.AnalyseToken);
        var input = await _contextBuilder.BuildContextAsync(contextRequest, cancellationToken);
        var sorted = RowSorter.Sort(BitmapBuilder.Build(input));
        var original = BitmapCloner.Clone(sorted);
        var working = BitmapCloner.Clone(sorted);

        var scorer = new HarmonyScorer();
        var fitness = new HarmonyFitnessEvaluator(scorer);
        var validator = new PlanMutationValidator(new DomainAwareReplaceValidator(input.Availability));
        var committee = new ConstraintAgentCommittee(new IConstraintAgent[]
        {
            new HoursConstraintAgent(),
            new PauseConstraintAgent(),
            new ConsecutiveConstraintAgent(),
            new RotationConstraintAgent(),
            new PreferenceConstraintAgent(),
        });
        var batchEvaluator = new BatchEvaluator(validator, fitness, committee);
        var rejectMemory = new RejectMemory();
        var cap = new AdaptiveBatchCap();
        var iterations = new List<BatchEvaluation>();
        var agentSummary = BuildAgentSummary(working);
        var fitnessBefore = fitness.Evaluate(original).Fitness;

        if (!ping.IsHealthy)
        {
            return new HolisticHarmonizerRunResult(
                OriginalBitmap: original,
                FinalBitmap: original,
                Iterations: iterations,
                FitnessBefore: fitnessBefore,
                FitnessAfter: fitnessBefore,
                LlmModelId: request.LlmModelId,
                LlmParsingError: $"Pre-flight check failed ({ping.LatencyMs} ms): {ping.Error}",
                LlmRawResponsePreview: null);
        }

        var stopwatch = Stopwatch.StartNew();
        var plateauCounter = 0;
        var bestFitness = fitnessBefore;
        var acceptedCountSoFar = 0;
        var rejectedCountSoFar = 0;
        string? lastParsingError = null;
        string? lastRawResponse = null;
        var pngRenderer = new HarmonyBitmapPngRenderer();

        progress?.Report(new HolisticHarmonizerProgress(
            IterationIndex: 0,
            MaxIterations: MaxInnerIterations,
            BestFitness: bestFitness,
            AcceptedBatchCount: 0,
            RejectedBatchCount: 0,
            ElapsedMs: 0));

        for (var iter = 0; iter < MaxInnerIterations; iter++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stopwatch.Elapsed > InnerLoopTimeBudget)
            {
                _logger.LogInformation("Holistic Harmonizer inner loop hit time budget after iter={Iter}", iter);
                break;
            }
            if (plateauCounter >= PlateauStopThreshold)
            {
                _logger.LogInformation("Holistic Harmonizer inner loop hit plateau after iter={Iter}", iter);
                break;
            }

            var proposalRequest = new PlanProposalRequest(
                ModelId: request.LlmModelId,
                PlanText: HarmonyBitmapTextRenderer.Render(working),
                AgentSummary: agentSummary,
                FragmentationSummary: FragmentationAnalyzer.Render(working),
                MaxStepsPerBatch: cap.Current,
                Language: request.Language ?? "en",
                IterationIndex: iter,
                PriorRejections: rejectMemory.Entries.ToArray(),
                PlanPng: pngRenderer.Render(working));

            var response = await _proposalProvider.ProposeAsync(proposalRequest, cancellationToken);
            lastRawResponse = response.RawResponse;

            if (response.ParsingError is not null)
            {
                lastParsingError = response.ParsingError;
                _logger.LogWarning("Holistic Harmonizer iter={Iter} parsing failed: {Error}", iter, response.ParsingError);
                plateauCounter++;
                continue;
            }

            if (response.Batches.Count == 0)
            {
                _logger.LogInformation("Holistic Harmonizer iter={Iter} returned 0 batches — LLM signals satisfied", iter);
                break;
            }

            var iterImproved = false;
            foreach (var batch in response.Batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var evaluation = batchEvaluator.Evaluate(working, batch);
                iterations.Add(evaluation);

                if (evaluation.Result == BatchAcceptance.Accepted || evaluation.Result == BatchAcceptance.PartiallyAccepted)
                {
                    cap.RecordAccept();
                    acceptedCountSoFar++;
                    if (evaluation.ScoreAfter > bestFitness)
                    {
                        bestFitness = evaluation.ScoreAfter;
                        iterImproved = true;
                    }
                }
                else
                {
                    cap.RecordReject();
                    rejectedCountSoFar++;
                    rejectMemory.Note(evaluation);
                }
            }

            if (iterImproved)
            {
                plateauCounter = 0;
            }
            else
            {
                plateauCounter++;
            }

            progress?.Report(new HolisticHarmonizerProgress(
                IterationIndex: iter + 1,
                MaxIterations: MaxInnerIterations,
                BestFitness: bestFitness,
                AcceptedBatchCount: acceptedCountSoFar,
                RejectedBatchCount: rejectedCountSoFar,
                ElapsedMs: stopwatch.ElapsedMilliseconds));
        }

        var fitnessAfter = fitness.Evaluate(working).Fitness;

        _logger.LogInformation(
            "Holistic Harmonizer run finished: model={Model} iterations={Iter} acceptedBatches={A} rejectedBatches={R} fitness {Before:F3} -> {After:F3} elapsed={Ms}ms",
            request.LlmModelId, iterations.Count, acceptedCountSoFar, rejectedCountSoFar, fitnessBefore, fitnessAfter, stopwatch.ElapsedMilliseconds);

        var rawPreview = lastRawResponse is not null && lastRawResponse.Length > RawResponsePreviewLength
            ? lastRawResponse[..RawResponsePreviewLength] + "..."
            : lastRawResponse;

        return new HolisticHarmonizerRunResult(
            OriginalBitmap: original,
            FinalBitmap: working,
            Iterations: iterations,
            FitnessBefore: fitnessBefore,
            FitnessAfter: fitnessAfter,
            LlmModelId: request.LlmModelId,
            LlmParsingError: lastParsingError,
            LlmRawResponsePreview: lastParsingError is not null ? rawPreview : null);
    }

    private static string BuildAgentSummary(HarmonyBitmap bitmap)
    {
        var sb = new StringBuilder();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var agent = bitmap.Rows[r];
            var preferred = string.Join(",", agent.PreferredShiftSymbols);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "r{0:D2} {1}: target={2}h maxWeekly={3}h maxConsec={4} minPause={5}h preferred=[{6}]",
                r,
                agent.DisplayName,
                agent.TargetHours,
                agent.MaxWeeklyHours,
                agent.MaxConsecutiveDays,
                agent.MinPauseHours,
                preferred));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
