// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Probes every enabled LLM model with a tiny text-only ping and reports availability, latency,
/// context window and per-token cost. Used by the assistant speech-settings UI to surface models
/// that are reachable, fast and cheap (preferably free) for transcription enhancement.
/// Model and provider resolution runs sequentially (scoped DbContext is not thread-safe);
/// the network pings themselves run concurrently with a bounded degree of parallelism and
/// one retry for transient failures.
/// </summary>
public sealed class SpeechModelCheckService
{
    private const int PingMaxTokens = 30;
    private const int ResponsePreviewLength = 80;
    private const int MaxConcurrentPings = 6;
    private const int MaxPingAttempts = 2;
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);
    private const string PingSystemPrompt =
        "You are a tiny availability probe. Reply with exactly the single word: ok\n" +
        "No prose, no punctuation, no quotes, no other words.";
    private const string PingUserMessage = "Reply with: ok";
    private const string ExpectedReply = "ok";

    private readonly ILLMRepository _llmRepository;
    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<SpeechModelCheckService> _logger;

    public SpeechModelCheckService(
        ILLMRepository llmRepository,
        LLMProviderOrchestrator orchestrator,
        ILogger<SpeechModelCheckService> logger)
    {
        _llmRepository = llmRepository;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SpeechModelCheckResult>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        if (models.Count == 0)
        {
            return [];
        }

        var targets = new List<PingTarget>(models.Count);
        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var displayName = string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName;
            var (resolvedModel, provider, error) = await _orchestrator.GetModelAndProviderAsync(model.ModelId);
            targets.Add(new PingTarget(model, displayName, resolvedModel, provider, error));
        }

        using var throttle = new SemaphoreSlim(MaxConcurrentPings);
        var pingTasks = targets.Select(async target =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                return await CheckSingleAsync(target, cancellationToken);
            }
            finally
            {
                throttle.Release();
            }
        }).ToArray();

        return await Task.WhenAll(pingTasks);
    }

    private async Task<SpeechModelCheckResult> CheckSingleAsync(PingTarget target, CancellationToken cancellationToken)
    {
        var model = target.Model;
        (bool IsHealthy, long LatencyMs, string? Error) ping;
        try
        {
            if (target.ResolvedModel is null || target.Provider is null)
            {
                ping = (false, 0, target.ResolutionError ?? "LLM provider unavailable.");
            }
            else
            {
                ping = await PingWithRetryAsync(target.ResolvedModel, target.Provider, model.ModelId, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Speech model check threw for {ModelId}", model.ModelId);
            ping = (false, 0, ex.Message);
        }

        return new SpeechModelCheckResult(
            ModelId: model.ModelId,
            DisplayName: target.DisplayName,
            ProviderId: model.ProviderId,
            IsHealthy: ping.IsHealthy,
            LatencyMs: ping.LatencyMs,
            ContextWindow: model.ContextWindow,
            CostPerInputToken: model.CostPerInputToken,
            CostPerOutputToken: model.CostPerOutputToken,
            Error: ping.Error);
    }

    private async Task<(bool IsHealthy, long LatencyMs, string? Error)> PingWithRetryAsync(
        LLMModel model,
        ILLMProvider provider,
        string modelId,
        CancellationToken cancellationToken)
    {
        (bool IsHealthy, long LatencyMs, string? Error, bool Retryable) attemptResult = default;
        for (var attempt = 1; attempt <= MaxPingAttempts; attempt++)
        {
            attemptResult = await PingOnceAsync(model, provider, modelId, cancellationToken);
            if (attemptResult.IsHealthy || !attemptResult.Retryable)
            {
                break;
            }

            if (attempt < MaxPingAttempts)
            {
                _logger.LogInformation(
                    "Speech model ping attempt {Attempt} failed for {ModelId}, retrying: {Error}",
                    attempt,
                    modelId,
                    attemptResult.Error);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        return (attemptResult.IsHealthy, attemptResult.LatencyMs, attemptResult.Error);
    }

    private async Task<(bool IsHealthy, long LatencyMs, string? Error, bool Retryable)> PingOnceAsync(
        LLMModel model,
        ILLMProvider provider,
        string modelId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var request = new LLMProviderRequest
        {
            Message = PingUserMessage,
            SystemPrompt = PingSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.0,
            MaxTokens = PingMaxTokens,
            ThinkingBudgetTokens = 0,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(PingTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(request, cts.Token);
            stopwatch.Stop();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return (false, stopwatch.ElapsedMilliseconds, $"Ping timed out after {PingTimeout.TotalSeconds:F0}s.", true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Speech model ping threw for {ModelId}", modelId);
            return (false, stopwatch.ElapsedMilliseconds, $"Ping failed: {ex.Message}", true);
        }

        if (!response.Success)
        {
            return (false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the ping.", true);
        }

        var content = response.Content ?? string.Empty;
        if (!ContainsExpectedReply(content))
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return (false, stopwatch.ElapsedMilliseconds, $"Model returned unexpected content: {preview}", false);
        }

        return (true, stopwatch.ElapsedMilliseconds, null, false);
    }

    private static bool ContainsExpectedReply(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }
        var normalised = content.Trim().ToLowerInvariant();
        return normalised.Contains(ExpectedReply, StringComparison.Ordinal);
    }

    private sealed record PingTarget(
        LLMModel Model,
        string DisplayName,
        LLMModel? ResolvedModel,
        ILLMProvider? Provider,
        string? ResolutionError);
}
