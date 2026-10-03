// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Logging;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.Scoring;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Application-layer entry point for Holistic Harmonizer runs. Reads the stage-3 mode (<c>WIZARD3_MODE</c>):
/// by default it runs <see cref="HolisticHarmonizerDeterministicEngine"/> (no LLM, no API key needed); with
/// mode "llm" it reads the configured LLM model id and invokes <see cref="HolisticHarmonizerEngine"/>. Either way
/// the resulting bitmap is stored in the shared <see cref="HarmonizerResultCache"/> under the job id so the
/// existing <see cref="IHarmonizerApplyService"/> can materialise it as a scenario without changes.
/// </summary>
public sealed class HolisticHarmonizerRunService
{
    private readonly HolisticHarmonizerEngine _engine;
    private readonly HolisticHarmonizerDeterministicEngine _deterministicEngine;
    private readonly HarmonizerResultCache _resultCache;
    private readonly ISettingsReader _settingsReader;
    private readonly IScheduleSnapshotMarkerService _snapshotMarkerService;
    private readonly ILogger<HolisticHarmonizerRunService> _logger;

    public HolisticHarmonizerRunService(
        HolisticHarmonizerEngine engine,
        HolisticHarmonizerDeterministicEngine deterministicEngine,
        HarmonizerResultCache resultCache,
        ISettingsReader settingsReader,
        IScheduleSnapshotMarkerService snapshotMarkerService,
        ILogger<HolisticHarmonizerRunService> logger)
    {
        _snapshotMarkerService = snapshotMarkerService;
        _engine = engine;
        _deterministicEngine = deterministicEngine;
        _resultCache = resultCache;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public Task<HolisticHarmonizerRunOutcome> RunAsync(HolisticHarmonizerRunInput input, CancellationToken cancellationToken)
        => RunAsync(input, jobId: null, progress: null, cancellationToken);

    public async Task<HolisticHarmonizerRunOutcome> RunAsync(
        HolisticHarmonizerRunInput input,
        Guid? jobId,
        IProgress<HolisticHarmonizerProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var modeSetting = await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_MODE);
        if (!HolisticHarmonizerModes.IsKnown(modeSetting?.Value))
        {
            _logger.LogWarning(
                "Holistic Harmonizer mode setting has an unknown value; falling back to {Mode}",
                HolisticHarmonizerModes.Deterministic);
        }
        var mode = HolisticHarmonizerModes.Parse(modeSetting?.Value);

        string? modelId = null;
        if (mode == HolisticHarmonizerMode.Llm)
        {
            var modelSetting = await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_LLM_MODEL);
            modelId = modelSetting?.Value;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return HolisticHarmonizerRunOutcome.Failure("Holistic Harmonizer LLM model is not configured. Open Settings → Work & Scheduling → Holistic Harmonizer to pick a model.");
            }
        }

        HolisticHarmonizerRunResult result;
        // Fingerprint of what this run is about to change; apply compares it again.
        var snapshotMarker = await _snapshotMarkerService.ComputeAsync(
            input.PeriodFrom, input.PeriodUntil, input.AgentIds, input.AnalyseToken, cancellationToken);

        try
        {
            result = mode == HolisticHarmonizerMode.Llm
                ? await _engine.RunAsync(BuildEngineRequest(input, modelId!), progress, cancellationToken)
                : await _deterministicEngine.RunAsync(input, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holistic Harmonizer engine failed for period {From}-{Until}", Convert.ToString(input.PeriodFrom).ForLog(), Convert.ToString(input.PeriodUntil).ForLog());
            return HolisticHarmonizerRunOutcome.Failure($"Holistic Harmonizer engine failed: {ex.Message}");
        }

        // A run that aborted on unusable model responses and never accepted a single batch produced
        // nothing worth caching - report it as a failure instead of a successful zero-improvement run.
        if (result.AbortedOnUnusableResponses
            && !result.Iterations.Any(i => i.Result is BatchAcceptance.Accepted or BatchAcceptance.PartiallyAccepted))
        {
            return HolisticHarmonizerRunOutcome.Failure(
                result.LlmParsingError ?? "Model produced no usable response.");
        }

        var resolvedJobId = jobId ?? Guid.NewGuid();

        // Bridge the score snapshot for the (deferred) preference-learner into the cache, like Wizard 1/2.
        // Wizard 3 has no assigned-unqualified scan here, so Stage0Violations stays 0 (a known signal gap).
        var subScoreJson = string.Empty;
        try
        {
            subScoreJson = EngineScoreSerializer.SerializeHolistic(result.FinalBitmap, result.FitnessAfter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Holistic Harmonizer score capture failed for job {JobId}; storing empty SubScoreJson", resolvedJobId);
        }

        _resultCache.Store(resolvedJobId, result.OriginalBitmap, result.FinalBitmap, input.AnalyseToken, subScoreJson, stage0Violations: 0, snapshotMarker);

        return HolisticHarmonizerRunOutcome.Success(resolvedJobId, result);
    }

    private static HolisticHarmonizerEngineRequest BuildEngineRequest(HolisticHarmonizerRunInput input, string modelId)
        => new(
            PeriodFrom: input.PeriodFrom,
            PeriodUntil: input.PeriodUntil,
            AgentIds: input.AgentIds,
            AnalyseToken: input.AnalyseToken,
            LlmModelId: modelId,
            Language: input.Language,
            ContextDaysBefore: input.ContextDaysBefore,
            ContextDaysAfter: input.ContextDaysAfter);
}
