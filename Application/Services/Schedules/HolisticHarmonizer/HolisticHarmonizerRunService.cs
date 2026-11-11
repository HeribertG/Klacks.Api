// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Logging;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.Scoring;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Application-layer entry point for Holistic Harmonizer runs. Runs <see cref="HolisticHarmonizerDeterministicEngine"/>
/// (no LLM, no API key needed) and stores the resulting bitmap in the shared <see cref="HarmonizerResultCache"/> under
/// the job id so the existing <see cref="IHarmonizerApplyService"/> can materialise it as a scenario without changes.
/// </summary>
/// <param name="deterministicEngine">The stage-3 local search.</param>
/// <param name="resultCache">Shared cache the apply pipeline reads the run result from.</param>
/// <param name="snapshotMarkerService">Fingerprints the plan the run is about to change; apply compares it again.</param>
/// <param name="logger">Logs engine failures and score capture problems.</param>
public sealed class HolisticHarmonizerRunService
{
    private readonly HolisticHarmonizerDeterministicEngine _deterministicEngine;
    private readonly HarmonizerResultCache _resultCache;
    private readonly IScheduleSnapshotMarkerService _snapshotMarkerService;
    private readonly ILogger<HolisticHarmonizerRunService> _logger;

    public HolisticHarmonizerRunService(
        HolisticHarmonizerDeterministicEngine deterministicEngine,
        HarmonizerResultCache resultCache,
        IScheduleSnapshotMarkerService snapshotMarkerService,
        ILogger<HolisticHarmonizerRunService> logger)
    {
        _deterministicEngine = deterministicEngine;
        _resultCache = resultCache;
        _snapshotMarkerService = snapshotMarkerService;
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

        HolisticHarmonizerRunResult result;
        // Fingerprint of what this run is about to change; apply compares it again.
        var snapshotMarker = await _snapshotMarkerService.ComputeAsync(
            input.PeriodFrom, input.PeriodUntil, input.AgentIds, input.AnalyseToken, cancellationToken);

        try
        {
            result = await _deterministicEngine.RunAsync(input, progress, cancellationToken);
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
}
