// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Probes every enabled LLM model with a tiny text-only ping and reports availability, latency,
/// context window and per-token cost. Used by the assistant speech-settings UI to surface models
/// that are reachable, fast and cheap (preferably free) for transcription enhancement.
/// </summary>
public sealed class SpeechModelCheckService
{
    private const int PingMaxTokens = 30;
    private const int ResponsePreviewLength = 80;
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(30);
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

        var results = new List<SpeechModelCheckResult>(models.Count);
        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var displayName = string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName;
            try
            {
                var ping = await PingAsync(model.ModelId, cancellationToken);
                results.Add(new SpeechModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: displayName,
                    ProviderId: model.ProviderId,
                    IsHealthy: ping.IsHealthy,
                    LatencyMs: ping.LatencyMs,
                    ContextWindow: model.ContextWindow,
                    CostPerInputToken: model.CostPerInputToken,
                    CostPerOutputToken: model.CostPerOutputToken,
                    Error: ping.Error));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Speech model check threw for {ModelId}", model.ModelId);
                results.Add(new SpeechModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: displayName,
                    ProviderId: model.ProviderId,
                    IsHealthy: false,
                    LatencyMs: 0,
                    ContextWindow: model.ContextWindow,
                    CostPerInputToken: model.CostPerInputToken,
                    CostPerOutputToken: model.CostPerOutputToken,
                    Error: ex.Message));
            }
        }

        return results;
    }

    private async Task<(bool IsHealthy, long LatencyMs, string? Error)> PingAsync(string modelId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return (false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var request = new LLMProviderRequest
        {
            Message = PingUserMessage,
            SystemPrompt = PingSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.0,
            MaxTokens = PingMaxTokens,
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
            return (false, stopwatch.ElapsedMilliseconds, $"Ping timed out after {PingTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Speech model ping threw for {ModelId}", modelId);
            return (false, stopwatch.ElapsedMilliseconds, $"Ping failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return (false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the ping.");
        }

        var content = response.Content ?? string.Empty;
        if (!ContainsExpectedReply(content))
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return (false, stopwatch.ElapsedMilliseconds, $"Model returned unexpected content: {preview}");
        }

        return (true, stopwatch.ElapsedMilliseconds, null);
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
}
