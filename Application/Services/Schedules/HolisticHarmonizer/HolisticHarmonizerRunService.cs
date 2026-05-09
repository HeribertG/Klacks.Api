// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Application-layer entry point for Holistic Harmonizer runs. Reads the configured LLM model id from
/// app settings, invokes <see cref="HolisticHarmonizerEngine"/>, stores the resulting bitmap in the shared
/// <see cref="HarmonizerResultCache"/> under a fresh job id so the existing
/// <see cref="IHarmonizerApplyService"/> can materialise it as a scenario without changes.
/// </summary>
public sealed class HolisticHarmonizerRunService
{
    private readonly HolisticHarmonizerEngine _engine;
    private readonly HarmonizerResultCache _resultCache;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<HolisticHarmonizerRunService> _logger;

    public HolisticHarmonizerRunService(
        HolisticHarmonizerEngine engine,
        HarmonizerResultCache resultCache,
        ISettingsReader settingsReader,
        ILogger<HolisticHarmonizerRunService> logger)
    {
        _engine = engine;
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

        var modelSetting = await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_LLM_MODEL);
        var modelId = modelSetting?.Value;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return HolisticHarmonizerRunOutcome.Failure("Holistic Harmonizer LLM model is not configured. Open Settings → Work & Scheduling → Holistic Harmonizer to pick a model.");
        }

        var engineRequest = new HolisticHarmonizerEngineRequest(
            PeriodFrom: input.PeriodFrom,
            PeriodUntil: input.PeriodUntil,
            AgentIds: input.AgentIds,
            AnalyseToken: input.AnalyseToken,
            LlmModelId: modelId,
            Language: input.Language,
            ContextDaysBefore: input.ContextDaysBefore,
            ContextDaysAfter: input.ContextDaysAfter);

        HolisticHarmonizerRunResult result;
        try
        {
            result = await _engine.RunAsync(engineRequest, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holistic Harmonizer engine failed for period {From}-{Until}", input.PeriodFrom, input.PeriodUntil);
            return HolisticHarmonizerRunOutcome.Failure($"Holistic Harmonizer engine failed: {ex.Message}");
        }

        var resolvedJobId = jobId ?? Guid.NewGuid();
        _resultCache.Store(resolvedJobId, result.OriginalBitmap, result.FinalBitmap, input.AnalyseToken);

        return HolisticHarmonizerRunOutcome.Success(resolvedJobId, result);
    }
}
