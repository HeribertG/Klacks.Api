// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces.Schedules;
using System.Globalization;
using System.Text;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Harmonizer.Scorer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Candidates;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Search;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Validation;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Holistic Harmonizer engine. Runs an inner LLM-ALNS loop: each iteration renders the current
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

    /// <summary>
    /// Consecutive iterations whose response was unparsable, signalled no batches without the satisfied flag, or
    /// contained no usable step after candidate filtering. Stops the run as failed instead of reporting a silent
    /// zero-improvement success.
    /// </summary>
    private const int MaxConsecutiveUnusableResponses = 3;
    private const int RawResponsePreviewLength = 600;
    private const string RawResponsePreviewEllipsis = "...";
    private const string DefaultLanguage = "en";
    private const string NoUsableBatchError = "Model response contained no usable batch.";
    private const string EmptyLogValue = "-";
    private const string SwapListSeparator = ",";
    private const string SwapLogFormat = "r{0}d{1}<->r{2}d{3}";
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

        var ping = await RunPreflightAsync(request.LlmModelId, cancellationToken);

        _logger.LogInformation(
            "Holistic Harmonizer pre-flight: model={Model} healthy={Healthy} latency={LatencyMs}ms error={Error}",
            request.LlmModelId, ping.IsHealthy, ping.LatencyMs, ping.Error ?? "<none>");

        var input = await _contextBuilder.BuildContextAsync(ToContextRequest(request), cancellationToken);
        var sorted = RowSorter.Sort(BitmapBuilder.Build(input));
        var original = BitmapCloner.Clone(sorted);
        var working = BitmapCloner.Clone(sorted);

        var components = HolisticHarmonizerComponents.Build(
            input, new HarmonyFitnessEvaluator(new HarmonyScorer()), MoveCandidatePool.DefaultTopPerIntent);
        var agentSummary = BuildAgentSummary(working);
        var fitnessBefore = components.Fitness.Evaluate(original).Fitness;

        if (!ping.IsHealthy)
        {
            return BuildPreflightFailedResult(request, original, fitnessBefore, ping);
        }

        var state = new HolisticHarmonizerRunState(request, working, components, agentSummary, fitnessBefore);
        await RunInnerLoopAsync(state, progress, cancellationToken);

        return BuildRunResult(state, input, original, fitnessBefore);
    }

    private static HarmonizerContextRequest ToContextRequest(HolisticHarmonizerEngineRequest request)
        => new(
            request.PeriodFrom,
            request.PeriodUntil,
            request.AgentIds,
            request.AnalyseToken,
            request.ContextDaysBefore,
            request.ContextDaysAfter);

    private static HolisticHarmonizerRunResult BuildPreflightFailedResult(
        HolisticHarmonizerEngineRequest request,
        HarmonyBitmap original,
        double fitnessBefore,
        PlanProposalPingResult ping)
        => new(
            OriginalBitmap: original,
            FinalBitmap: original,
            Iterations: new List<BatchEvaluation>(),
            FitnessBefore: fitnessBefore,
            FitnessAfter: fitnessBefore,
            LlmModelId: request.LlmModelId,
            LlmParsingError: $"Pre-flight check failed ({ping.LatencyMs} ms): {ping.Error}",
            LlmRawResponsePreview: null,
            AbortedOnUnusableResponses: true);

    /// <summary>
    /// The inner loop: one start tick, then up to <see cref="MaxInnerIterations"/> iterations until the time budget,
    /// the plateau, the LLM's satisfied signal or too many unusable responses stop it. Only a completed iteration
    /// reports progress; an iteration skipped for an unusable response does not.
    /// </summary>
    private async Task RunInnerLoopAsync(
        HolisticHarmonizerRunState state,
        IProgress<HolisticHarmonizerProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new HolisticHarmonizerProgress(
            IterationIndex: 0,
            MaxIterations: MaxInnerIterations,
            BestFitness: state.BestFitness,
            AcceptedBatchCount: 0,
            RejectedBatchCount: 0,
            ElapsedMs: 0));

        for (var iter = 0; iter < MaxInnerIterations; iter++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsBudgetOrPlateauReached(state, iter))
            {
                break;
            }

            var outcome = await RunIterationAsync(state, iter, cancellationToken);
            if (outcome == HolisticHarmonizerIterationOutcome.Stop)
            {
                break;
            }

            if (outcome == HolisticHarmonizerIterationOutcome.Completed)
            {
                ReportIterationProgress(state, progress, iter + 1);
            }
        }
    }

    private bool IsBudgetOrPlateauReached(HolisticHarmonizerRunState state, int iter)
    {
        if (state.Stopwatch.Elapsed > InnerLoopTimeBudget)
        {
            _logger.LogInformation("Holistic Harmonizer inner loop hit time budget after iter={Iter}", iter);
            return true;
        }

        if (state.PlateauCounter >= PlateauStopThreshold)
        {
            _logger.LogInformation("Holistic Harmonizer inner loop hit plateau after iter={Iter}", iter);
            return true;
        }

        return false;
    }

    private static void ReportIterationProgress(
        HolisticHarmonizerRunState state,
        IProgress<HolisticHarmonizerProgress>? progress,
        int iterationIndex)
        => progress?.Report(new HolisticHarmonizerProgress(
            IterationIndex: iterationIndex,
            MaxIterations: MaxInnerIterations,
            BestFitness: state.BestFitness,
            AcceptedBatchCount: state.AcceptedCount,
            RejectedBatchCount: state.RejectedCount,
            ElapsedMs: state.Stopwatch.ElapsedMilliseconds));

    /// <summary>
    /// One iteration: pick the focused intent, offer its candidates, ask the LLM and evaluate what came back.
    /// </summary>
    private async Task<HolisticHarmonizerIterationOutcome> RunIterationAsync(
        HolisticHarmonizerRunState state,
        int iter,
        CancellationToken cancellationToken)
    {
        var focusedIntent = PickFocusedIntent(state, iter);
        var excludedSwaps = state.RejectMemory.SameDayForbiddenSwapKeys();
        var iterCandidates = state.Components.Pool.Generate(state.Working, focusedIntent, excludedSwaps);
        _logger.LogInformation(
            "Holistic Harmonizer iter={Iter} intent={Intent} candidates={Count} excludedByRejectMemory={Excluded}",
            iter, focusedIntent, iterCandidates.Count, excludedSwaps.Count);

        var proposalRequest = BuildProposalRequest(state, iter, focusedIntent, iterCandidates);
        var response = await _proposalProvider.ProposeAsync(proposalRequest, cancellationToken);
        state.LastRawResponse = response.RawResponse;

        if (response.ParsingError is not null)
        {
            state.LastParsingError = response.ParsingError;
            _logger.LogWarning("Holistic Harmonizer iter={Iter} parsing failed: {Error}", iter, response.ParsingError);
            return RegisterUnusableResponse(state);
        }

        if (response.Batches.Count == 0)
        {
            return HandleResponseWithoutBatches(state, iter, response);
        }

        var (iterImproved, evaluatedAnyBatch) = EvaluateBatches(state, iter, response.Batches, iterCandidates, cancellationToken);
        return CompleteIteration(state, iterImproved, evaluatedAnyBatch);
    }

    /// <summary>
    /// Iteration 0 always uses the proven baseline intent so the first batch produced by the LLM has the best chance to
    /// land. From iteration 1 onward the roulette explores alternative intents weighted by their Laplace-smoothed
    /// success rate.
    /// </summary>
    private static string PickFocusedIntent(HolisticHarmonizerRunState state, int iter)
        => iter == 0
            ? HolisticIntent.ConsolidateBlock
            : state.IntentSelector.Pick(HolisticIntent.All, state.IntentTracker);

    private static PlanProposalRequest BuildProposalRequest(
        HolisticHarmonizerRunState state,
        int iter,
        string focusedIntent,
        IReadOnlyList<MoveCandidate> iterCandidates)
        => new(
            ModelId: state.Request.LlmModelId,
            PlanText: HarmonyBitmapTextRenderer.Render(state.Working),
            AgentSummary: state.AgentSummary,
            FragmentationSummary: FragmentationAnalyzer.Render(state.Working),
            MaxStepsPerBatch: state.Cap.Current,
            Language: state.Request.Language ?? DefaultLanguage,
            IterationIndex: iter,
            PriorRejections: state.RejectMemory.Entries.ToArray(),
            PlanPng: state.PngRenderer.Render(state.Working),
            FocusedIntent: focusedIntent,
            CandidateMoves: iterCandidates);

    /// <summary>
    /// An empty batches array ends the run when the LLM signals satisfaction. Without that signal no batch survived
    /// parsing and the model never signalled convergence either; treating that as success is how a broken model used
    /// to look like a finished run, so it counts as an unusable response.
    /// </summary>
    private HolisticHarmonizerIterationOutcome HandleResponseWithoutBatches(
        HolisticHarmonizerRunState state,
        int iter,
        PlanProposalResponse response)
    {
        if (response.LlmSignaledSatisfied)
        {
            _logger.LogInformation("Holistic Harmonizer iter={Iter} returned an empty batches array — LLM signals satisfied", iter);
            return HolisticHarmonizerIterationOutcome.Stop;
        }

        state.LastParsingError = NoUsableBatchError;
        _logger.LogWarning("Holistic Harmonizer iter={Iter} produced no usable batch without signalling satisfaction", iter);
        return RegisterUnusableResponse(state);
    }

    /// <summary>
    /// An unparsable response or one without batches counts toward the plateau and toward the unusable-response limit;
    /// the iteration is skipped without a progress report, or the run aborts once the limit is reached.
    /// </summary>
    private static HolisticHarmonizerIterationOutcome RegisterUnusableResponse(HolisticHarmonizerRunState state)
    {
        state.PlateauCounter++;
        return CountUnusableAndCheckAbort(state)
            ? HolisticHarmonizerIterationOutcome.Stop
            : HolisticHarmonizerIterationOutcome.Skipped;
    }

    /// <summary>
    /// Extends the streak of unusable iterations and marks the run as aborted once it reaches
    /// <see cref="MaxConsecutiveUnusableResponses"/>.
    /// </summary>
    /// <returns>True when the run has to stop.</returns>
    private static bool CountUnusableAndCheckAbort(HolisticHarmonizerRunState state)
    {
        state.ConsecutiveUnusable++;
        if (state.ConsecutiveUnusable < MaxConsecutiveUnusableResponses)
        {
            return false;
        }

        state.AbortedOnUnusableResponses = true;
        return true;
    }

    private (bool IterImproved, bool EvaluatedAnyBatch) EvaluateBatches(
        HolisticHarmonizerRunState state,
        int iter,
        IReadOnlyList<MutationBatch> batches,
        IReadOnlyList<MoveCandidate> iterCandidates,
        CancellationToken cancellationToken)
    {
        var iterImproved = false;
        var evaluatedAnyBatch = false;
        foreach (var rawBatch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = FilterToCandidates(iter, rawBatch, iterCandidates);
            if (batch.Steps.Count == 0)
            {
                continue;
            }

            var evaluation = state.Components.Evaluator.Evaluate(state.Working, batch);
            evaluatedAnyBatch = true;
            state.Iterations.Add(evaluation);
            LogBatchOutcome(iter, evaluation);

            state.IntentTracker.Note(batch.Intent, evaluation.Result);

            if (RecordEvaluation(state, evaluation))
            {
                iterImproved = true;
            }
        }

        return (iterImproved, evaluatedAnyBatch);
    }

    /// <summary>
    /// Strict-candidate mode: steps the LLM proposed outside the candidate list are dropped before the evaluation.
    /// </summary>
    private MutationBatch FilterToCandidates(int iter, MutationBatch rawBatch, IReadOnlyList<MoveCandidate> iterCandidates)
    {
        var fromCandidates = CandidateStepFilter.CountStepsInCandidates(rawBatch, iterCandidates);
        var batch = CandidateStepFilter.FilterToCandidates(rawBatch, iterCandidates);
        var droppedSelfProposed = rawBatch.Steps.Count - batch.Steps.Count;
        _logger.LogInformation(
            "Holistic Harmonizer iter={Iter} batch intent={Intent} steps={Steps} fromCandidates={Hits}/{Total} droppedSelfProposed={Dropped}",
            iter, rawBatch.Intent, rawBatch.Steps.Count, fromCandidates, rawBatch.Steps.Count, droppedSelfProposed);

        if (batch.Steps.Count == 0 && droppedSelfProposed > 0)
        {
            _logger.LogInformation(
                "Holistic Harmonizer iter={Iter} batch intent={Intent} skipped: all steps were self-proposed and dropped (strict-candidate mode)",
                iter, rawBatch.Intent);
        }

        return batch;
    }

    /// <summary>
    /// Feeds one evaluation into the batch cap, the counters and (for a reject) the reject memory.
    /// </summary>
    /// <returns>True when the evaluation raised the best fitness of the run.</returns>
    private static bool RecordEvaluation(HolisticHarmonizerRunState state, BatchEvaluation evaluation)
    {
        if (evaluation.Result == BatchAcceptance.Accepted || evaluation.Result == BatchAcceptance.PartiallyAccepted)
        {
            state.Cap.RecordAccept();
            state.AcceptedCount++;
            if (evaluation.ScoreAfter > state.BestFitness)
            {
                state.BestFitness = evaluation.ScoreAfter;
                return true;
            }

            return false;
        }

        state.Cap.RecordReject();
        state.RejectedCount++;
        state.RejectMemory.Note(evaluation);
        return false;
    }

    /// <summary>
    /// When every step was dropped by the candidate filter nothing was evaluated, so the iteration is as unusable as an
    /// unparsable response (but it does not count toward the plateau twice). Otherwise the unusable streak and the last
    /// parsing error are reset; the plateau counter follows the best-fitness improvement.
    /// </summary>
    private static HolisticHarmonizerIterationOutcome CompleteIteration(
        HolisticHarmonizerRunState state,
        bool iterImproved,
        bool evaluatedAnyBatch)
    {
        if (evaluatedAnyBatch)
        {
            state.ConsecutiveUnusable = 0;
            state.LastParsingError = null;
        }
        else if (CountUnusableAndCheckAbort(state))
        {
            return HolisticHarmonizerIterationOutcome.Stop;
        }

        state.PlateauCounter = iterImproved ? 0 : state.PlateauCounter + 1;
        return HolisticHarmonizerIterationOutcome.Completed;
    }

    private HolisticHarmonizerRunResult BuildRunResult(
        HolisticHarmonizerRunState state,
        BitmapInput input,
        HarmonyBitmap original,
        double fitnessBefore)
    {
        var fitnessAfter = state.Components.Fitness.Evaluate(state.Working).Fitness;

        _logger.LogInformation(
            "Holistic Harmonizer run finished: model={Model} iterations={Iter} acceptedBatches={A} rejectedBatches={R} fitness {Before:F3} -> {After:F3} elapsed={Ms}ms",
            state.Request.LlmModelId, state.Iterations.Count, state.AcceptedCount, state.RejectedCount, fitnessBefore, fitnessAfter, state.Stopwatch.ElapsedMilliseconds);

        var rawPreview = BuildRawResponsePreview(state.LastRawResponse);
        var lastParsingError = state.AbortedOnUnusableResponses && state.LastParsingError is null
            ? $"Model produced {MaxConsecutiveUnusableResponses} consecutive responses without a usable batch."
            : state.LastParsingError;

        return new HolisticHarmonizerRunResult(
            OriginalBitmap: original,
            FinalBitmap: state.Working,
            Iterations: state.Iterations,
            FitnessBefore: fitnessBefore,
            FitnessAfter: fitnessAfter,
            LlmModelId: state.Request.LlmModelId,
            LlmParsingError: lastParsingError,
            LlmRawResponsePreview: lastParsingError is not null ? rawPreview : null,
            AbortedOnUnusableResponses: state.AbortedOnUnusableResponses)
        {
            InvalidPlanningRuleIds = input.Rules?.InvalidHardRuleIds ?? [],
            PlanningRuleHardCounts = state.Components.Rules?.CountHard(original, state.Working),
        };
    }

    private static string? BuildRawResponsePreview(string? lastRawResponse)
        => lastRawResponse is not null && lastRawResponse.Length > RawResponsePreviewLength
            ? lastRawResponse[..RawResponsePreviewLength] + RawResponsePreviewEllipsis
            : lastRawResponse;

    /// <summary>
    /// A text ping only proves the model answers at all - a text-only model passes it and then fails on every
    /// bitmap. On a cache miss the (expensive, up to 90 s) capability round-trip runs instead and subsumes the
    /// ping; on the first run per model that shortens the inner loop, which is accepted because the verdict is
    /// then cached for hours. Only a real verdict is cached: a timeout or an unavailable provider says nothing
    /// about vision, and caching it as "not vision-capable" would make readers treat a transient outage as a
    /// text-only model.
    /// </summary>
    private async Task<PlanProposalPingResult> RunPreflightAsync(string modelId, CancellationToken cancellationToken)
    {
        if (_capabilityCache.TryGet(modelId, out var cachedCapable, out var cachedError))
        {
            return cachedCapable
                ? await _proposalProvider.PingAsync(modelId, cancellationToken)
                : new PlanProposalPingResult(IsHealthy: false, LatencyMs: 0, Error: $"model is not vision-capable: {cachedError}", AnsweredButFailedImageCheck: true);
        }

        var ping = await _proposalProvider.CapabilityCheckAsync(modelId, cancellationToken);
        if (ping.IsHealthy || ping.AnsweredButFailedImageCheck)
        {
            _capabilityCache.Store(modelId, ping.IsHealthy, ping.Error);
        }

        return ping;
    }

    /// <summary>
    /// One structured line per evaluated batch. Only coordinates, reason enums and scores are logged -
    /// rejection details carry employee display names and stay out of the log.
    /// </summary>
    private void LogBatchOutcome(int iter, BatchEvaluation evaluation)
    {
        var firstRejection = evaluation.Rejections.Count > 0 ? evaluation.Rejections[0] : null;
        _logger.LogInformation(
            "Holistic Harmonizer iter={Iter} batch={BatchId} intent={Intent} result={Result} reason={Reason} stoppedAtStep={StoppedAt} rejectedAt={RejectedAt} applied={Applied} reverted={Reverted} score {Before:F4} -> {After:F4}",
            iter,
            evaluation.BatchId,
            evaluation.Intent,
            evaluation.Result,
            firstRejection?.Reason.ToString() ?? EmptyLogValue,
            evaluation.StoppedAtStep,
            firstRejection is null ? EmptyLogValue : FormatSwap(firstRejection.Swap),
            FormatSwaps(evaluation.AppliedSteps),
            FormatSwaps(evaluation.RevertedSteps),
            evaluation.ScoreBefore,
            evaluation.ScoreAfter);
    }

    private static string FormatSwaps(IReadOnlyList<PlanCellSwap> swaps)
        => swaps.Count == 0 ? EmptyLogValue : string.Join(SwapListSeparator, swaps.Select(FormatSwap));

    private static string FormatSwap(PlanCellSwap swap)
        => string.Format(CultureInfo.InvariantCulture, SwapLogFormat, swap.RowA, swap.DayA, swap.RowB, swap.DayB);

    internal static string BuildAgentSummary(HarmonyBitmap bitmap)
    {
        var sb = new StringBuilder();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var agent = bitmap.Rows[r];
            var preferred = string.Join(",", agent.PreferredShiftSymbols);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "r{0:D2} {1}: target={2:0.#}h maxWeekly={3}h maxConsec={4} minPause={5}h minRestDaysPerWeek={6} preferred=[{7}]",
                r,
                agent.DisplayName,
                agent.TargetHours,
                agent.MaxWeeklyHours,
                agent.MaxConsecutiveDays,
                agent.MinPauseHours,
                agent.MinRestDays,
                preferred));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
