// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.ScheduleOptimizer.Wizard3.Mutations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.Wizard3;

/// <summary>
/// Application-layer entry point for Wizard 3 runs. Reads the configured LLM model id from
/// app settings, invokes <see cref="Wizard3Engine"/>, stores the resulting bitmap in the shared
/// <see cref="HarmonizerResultCache"/> under a fresh job id so the existing
/// <see cref="IHarmonizerApplyService"/> can materialise it as a scenario without changes.
/// </summary>
public sealed class Wizard3RunService
{
    private readonly Wizard3Engine _engine;
    private readonly HarmonizerResultCache _resultCache;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<Wizard3RunService> _logger;

    public Wizard3RunService(
        Wizard3Engine engine,
        HarmonizerResultCache resultCache,
        ISettingsReader settingsReader,
        ILogger<Wizard3RunService> logger)
    {
        _engine = engine;
        _resultCache = resultCache;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<Wizard3RunOutcome> RunAsync(Wizard3RunInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var modelSetting = await _settingsReader.GetSetting(Settings.WIZARD3_LLM_MODEL);
        var modelId = modelSetting?.Value;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return Wizard3RunOutcome.Failure("Wizard 3 LLM model is not configured. Open Settings → Work & Scheduling → Wizard 3 to pick a model.");
        }

        var engineRequest = new Wizard3EngineRequest(
            PeriodFrom: input.PeriodFrom,
            PeriodUntil: input.PeriodUntil,
            AgentIds: input.AgentIds,
            AnalyseToken: input.AnalyseToken,
            LlmModelId: modelId,
            Language: input.Language);

        Wizard3RunResult result;
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
            _logger.LogError(ex, "Wizard 3 engine failed for period {From}-{Until}", input.PeriodFrom, input.PeriodUntil);
            return Wizard3RunOutcome.Failure($"Wizard 3 engine failed: {ex.Message}");
        }

        var jobId = Guid.NewGuid();
        _resultCache.Store(jobId, result.OriginalBitmap, result.FinalBitmap, input.AnalyseToken);

        return Wizard3RunOutcome.Success(jobId, result);
    }
}

/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="Language">UI language; null falls back to English.</param>
public sealed record Wizard3RunInput(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language);

/// <summary>
/// Outcome of <see cref="Wizard3RunService.RunAsync"/>: either a successful run with cached
/// result and job id (ready to apply as a scenario), or a failure message for the UI.
/// </summary>
public sealed record Wizard3RunOutcome
{
    public bool IsSuccess { get; init; }
    public Guid? JobId { get; init; }
    public Wizard3RunResult? Result { get; init; }
    public string? FailureMessage { get; init; }

    public static Wizard3RunOutcome Success(Guid jobId, Wizard3RunResult result)
        => new() { IsSuccess = true, JobId = jobId, Result = result };

    public static Wizard3RunOutcome Failure(string message)
        => new() { IsSuccess = false, FailureMessage = message };
}
