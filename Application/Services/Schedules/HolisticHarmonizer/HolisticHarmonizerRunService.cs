// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
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

    public async Task<HolisticHarmonizerRunOutcome> RunAsync(HolisticHarmonizerRunInput input, CancellationToken cancellationToken)
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
            Language: input.Language);

        HolisticHarmonizerRunResult result;
        try
        {
            result = await _engine.RunAsync(engineRequest, cancellationToken);
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

        var jobId = Guid.NewGuid();
        _resultCache.Store(jobId, result.OriginalBitmap, result.FinalBitmap, input.AnalyseToken);

        return HolisticHarmonizerRunOutcome.Success(jobId, result);
    }
}

/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="Language">UI language; null falls back to English.</param>
public sealed record HolisticHarmonizerRunInput(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language);

/// <summary>
/// Outcome of <see cref="HolisticHarmonizerRunService.RunAsync"/>: either a successful run with cached
/// result and job id (ready to apply as a scenario), or a failure message for the UI.
/// </summary>
public sealed record HolisticHarmonizerRunOutcome
{
    public bool IsSuccess { get; init; }
    public Guid? JobId { get; init; }
    public HolisticHarmonizerRunResult? Result { get; init; }
    public string? FailureMessage { get; init; }

    public static HolisticHarmonizerRunOutcome Success(Guid jobId, HolisticHarmonizerRunResult result)
        => new() { IsSuccess = true, JobId = jobId, Result = result };

    public static HolisticHarmonizerRunOutcome Failure(string message)
        => new() { IsSuccess = false, FailureMessage = message };
}
