// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces.Schedules;
using System.Diagnostics;
using System.Globalization;
using System.Text;
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

    /// <summary>
    /// Number of iterations without best-fitness improvement before the inner loop bails out.
    /// Raised from 3 to 5 because empirical Haiku runs frequently produce 1-2 dud iterations
    /// (same-symbol swaps, hard-cap violations) followed by a recovery iteration with a real
    /// improvement; stopping after 3 cuts off too many of those recoveries.
    /// </summary>
    private const int PlateauStopThreshold = 5;

    // Consecutive iterations whose response was unparsable, signalled no batches without the satisfied
    // flag, or contained no usable step after candidate filtering. Stops the run as failed instead of
    // reporting a silent zero-improvement success.
    private const int MaxConsecutiveUnusableResponses = 3;
    private const int RawResponsePreviewLength = 600;
    private static readonly TimeSpan InnerLoopTimeBudget = TimeSpan.FromSeconds(90);

    private readonly IHarmonizerContextBuilder _contextBuilder;
    private readonly IPlanProposalProvider _proposalProvider;
    private readonly HolisticHarmonizerModelCapabilityCache _capabilityCache;
    private readonly ILogger<HolisticHarmonizerEngine> _logger;

    public HolisticHarmonizerEngine(
        IHarmonizerContextBuilder contextBuilder,
        IPlanProposalProvider proposalProvider,
        HolisticHarmonizerModelCapabilityCache capabilityCache,
        ILogger<HolisticHarmonizerEngine> logger)
    {
        _contextBuilder = contextBuilder;
        _proposalProvider = proposalProvider;
        _capabilityCache = capabilityCache;
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

        // A text ping only proves the model answers at all - a text-only model passes it and then fails
        // on every bitmap. On a cache miss the (expensive, up to 90 s) capability round-trip runs instead
        // and subsumes the ping; on the first run per model that shortens the inner loop, which is
        // accepted because the verdict is then cached for hours.
        PlanProposalPingResult ping;
        if (_capabilityCache.TryGet(request.LlmModelId, out var cachedCapable, out var cachedError))
        {
            ping = cachedCapable
                ? await _proposalProvider.PingAsync(request.LlmModelId, cancellationToken)
                : new PlanProposalPingResult(IsHealthy: false, LatencyMs: 0, Error: $"model is not vision-capable: {cachedError}");
        }
        else
        {
            ping = await _proposalProvider.CapabilityCheckAsync(request.LlmModelId, cancellationToken);
            _capabilityCache.Store(request.LlmModelId, ping.IsHealthy, ping.Error);
        }

        _logger.LogInformation(
            "Holistic Harmonizer pre-flight: model={Model} healthy={Healthy} latency={LatencyMs}ms error={Error}",
            request.LlmModelId, ping.IsHealthy, ping.LatencyMs, ping.Error ?? "<none>");

        var contextRequest = new HarmonizerContextRequest(
            request.PeriodFrom,
            request.PeriodUntil,
            request.AgentIds,
            request.AnalyseToken,
            request.ContextDaysBefore,
            request.ContextDaysAfter);
        var input = await _contextBuilder.BuildContextAsync(contextRequest, cancellationToken);
        var sorted = RowSorter.Sort(BitmapBuilder.Build(input));
        var original = BitmapCloner.Clone(sorted);
        var working = BitmapCloner.Clone(sorted);

        var scorer = new HarmonyScorer();
        var fitness = new HarmonyFitnessEvaluator(scorer);
        var validator = new PlanMutationValidator(
            new DomainAwareReplaceValidator(input.Availability, input.BoundaryAssignments, input.IneligibleAssignments),
            input.RestrictedTimeWindows);
        var committee = new ConstraintAgentCommittee(new IConstraintAgent[]
        {
            new HoursConstraintAgent(),
            new PauseConstraintAgent(input.BoundaryAssignments),
            new ConsecutiveConstraintAgent(input.BoundaryAssignments),
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
                LlmRawResponsePreview: null,
                AbortedOnUnusableResponses: true);
        }

        var stopwatch = Stopwatch.StartNew();
        var plateauCounter = 0;
        var consecutiveUnusable = 0;
        var abortedOnUnusableResponses = false;
        var bestFitness = fitnessBefore;
        var acceptedCountSoFar = 0;
        var rejectedCountSoFar = 0;
        string? lastParsingError = null;
        string? lastRawResponse = null;
        var pngRenderer = new HarmonyBitmapPngRenderer();
        var intentTracker = new IntentSuccessTracker();
        var intentSelector = new RouletteIntentSelector();

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

            // Iteration 0 always uses the proven baseline intent so the first batch produced by
            // the LLM has the best chance to land. From iteration 1 onward the roulette explores
            // alternative intents weighted by their Laplace-smoothed success rate.
            var focusedIntent = iter == 0
                ? HolisticIntent.ConsolidateBlock
                : intentSelector.Pick(HolisticIntent.All, intentTracker);
            var iterCandidates = candidatePool.Generate(working, focusedIntent);
            _logger.LogInformation(
                "Holistic Harmonizer iter={Iter} intent={Intent} candidates={Count}",
                iter, focusedIntent, iterCandidates.Count);

            var proposalRequest = new PlanProposalRequest(
                ModelId: request.LlmModelId,
                PlanText: HarmonyBitmapTextRenderer.Render(working),
                AgentSummary: agentSummary,
                FragmentationSummary: FragmentationAnalyzer.Render(working),
                MaxStepsPerBatch: cap.Current,
                Language: request.Language ?? "en",
                IterationIndex: iter,
                PriorRejections: rejectMemory.Entries.ToArray(),
                PlanPng: pngRenderer.Render(working),
                FocusedIntent: focusedIntent,
                CandidateMoves: iterCandidates);

            var response = await _proposalProvider.ProposeAsync(proposalRequest, cancellationToken);
            lastRawResponse = response.RawResponse;

            if (response.ParsingError is not null)
            {
                lastParsingError = response.ParsingError;
                _logger.LogWarning("Holistic Harmonizer iter={Iter} parsing failed: {Error}", iter, response.ParsingError);
                plateauCounter++;
                consecutiveUnusable++;
                if (consecutiveUnusable >= MaxConsecutiveUnusableResponses)
                {
                    abortedOnUnusableResponses = true;
                    break;
                }
                continue;
            }

            if (response.Batches.Count == 0)
            {
                if (response.LlmSignaledSatisfied)
                {
                    _logger.LogInformation("Holistic Harmonizer iter={Iter} returned an empty batches array — LLM signals satisfied", iter);
                    break;
                }

                // No batch survived parsing, but the model never signalled convergence either.
                // Treating that as success is how a broken model used to look like a finished run.
                lastParsingError = "Model response contained no usable batch.";
                _logger.LogWarning("Holistic Harmonizer iter={Iter} produced no usable batch without signalling satisfaction", iter);
                plateauCounter++;
                consecutiveUnusable++;
                if (consecutiveUnusable >= MaxConsecutiveUnusableResponses)
                {
                    abortedOnUnusableResponses = true;
                    break;
                }
                continue;
            }

            var iterImproved = false;
            var evaluatedAnyBatch = false;
            foreach (var rawBatch in response.Batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fromCandidates = CandidateStepFilter.CountStepsInCandidates(rawBatch, iterCandidates);
                var batch = CandidateStepFilter.FilterToCandidates(rawBatch, iterCandidates);
                var droppedSelfProposed = rawBatch.Steps.Count - batch.Steps.Count;
                _logger.LogInformation(
                    "Holistic Harmonizer iter={Iter} batch intent={Intent} steps={Steps} fromCandidates={Hits}/{Total} droppedSelfProposed={Dropped}",
                    iter, rawBatch.Intent, rawBatch.Steps.Count, fromCandidates, rawBatch.Steps.Count, droppedSelfProposed);

                if (batch.Steps.Count == 0)
                {
                    if (droppedSelfProposed > 0)
                    {
                        _logger.LogInformation(
                            "Holistic Harmonizer iter={Iter} batch intent={Intent} skipped: all steps were self-proposed and dropped (strict-candidate mode)",
                            iter, rawBatch.Intent);
                    }
                    continue;
                }

                var evaluation = batchEvaluator.Evaluate(working, batch);
                evaluatedAnyBatch = true;
                iterations.Add(evaluation);

                intentTracker.Note(batch.Intent, evaluation.Result);

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

            if (evaluatedAnyBatch)
            {
                consecutiveUnusable = 0;
                lastParsingError = null;
            }
            else
            {
                // Every step was dropped by the candidate filter - nothing was evaluated, so this
                // iteration is as unusable as an unparsable response.
                consecutiveUnusable++;
                if (consecutiveUnusable >= MaxConsecutiveUnusableResponses)
                {
                    abortedOnUnusableResponses = true;
                    break;
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

        if (abortedOnUnusableResponses && lastParsingError is null)
        {
            lastParsingError =
                $"Model produced {MaxConsecutiveUnusableResponses} consecutive responses without a usable batch.";
        }

        return new HolisticHarmonizerRunResult(
            OriginalBitmap: original,
            FinalBitmap: working,
            Iterations: iterations,
            FitnessBefore: fitnessBefore,
            FitnessAfter: fitnessAfter,
            LlmModelId: request.LlmModelId,
            LlmParsingError: lastParsingError,
            LlmRawResponsePreview: lastParsingError is not null ? rawPreview : null,
            AbortedOnUnusableResponses: abortedOnUnusableResponses);
    }

    internal static string BuildAgentSummary(HarmonyBitmap bitmap)
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
