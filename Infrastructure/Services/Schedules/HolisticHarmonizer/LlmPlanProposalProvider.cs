// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default implementation of <see cref="IPlanProposalProvider"/>. Bypasses the conversational
/// <c>ILLMService</c> (which mixes Klacks system prompts, conversation history and tool
/// calling into every request) and instead drives the underlying <see cref="ILLMProvider"/>
/// directly so the LLM receives only Holistic Harmonizer's structured prompt and replies with the JSON
/// we expect.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

public sealed class LlmPlanProposalProvider : IPlanProposalProvider
{
    private const double ProposalTemperature = 0.2;
    private const int ProposalMaxTokens = 6000;
    private const int ResponsePreviewLength = 120;
    private static readonly TimeSpan ProposalTimeout = TimeSpan.FromSeconds(60);

    private const int PingMaxTokens = 50;
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(30);
    private const string PingSystemPrompt =
        "You are a JSON-only test endpoint. Reply with exactly: {\"ping\":\"pong\"}\n" +
        "No prose, no markdown, no commentary, no extra fields.";
    private const string PingUserMessage = "Reply with the JSON object as instructed.";

    private const int CapabilityMaxTokens = 2000;
    private static readonly TimeSpan CapabilityTimeout = TimeSpan.FromSeconds(90);
    private const string CapabilitySystemPrompt =
        "You are a deterministic schedule-harmonizer assistant for a capability self-test.\n" +
        "Reply with ONE JSON object and nothing else: {\"swaps\":[{\"rowA\":int,\"dayA\":int,\"rowB\":int,\"dayB\":int,\"reason\":string}, ...]}\n" +
        "No prose, no markdown, no code fences. Cells marked with * are LOCKED — never swap them.\n" +
        "Constraint: dayA must equal dayB (only same-day swaps).";
    private const string CapabilityUserMessage =
        "Mini schedule (3 employees × 5 days). Symbols: E=Early, L=Late, _=Free, * after symbol = locked.\n" +
        "       d0    d1    d2    d3    d4\n" +
        "r00    L     L     L     L     L\n" +
        "r01    _     _     _     _     _\n" +
        "r02    E     E     E     E     E\n" +
        "\n" +
        "Constraints: r00 max 3 consecutive Late; r01 target 30h; r02 max 3 consecutive Early.\n" +
        "r00 has 5 Late shifts in a row — propose at least one same-day swap that redistributes the load.\n" +
        "Reply with the JSON object only. dayA must equal dayB.";

    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<LlmPlanProposalProvider> _logger;

    public LlmPlanProposalProvider(LLMProviderOrchestrator orchestrator, ILogger<LlmPlanProposalProvider> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<PlanProposalPingResult> PingAsync(string modelId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var pingRequest = new LLMProviderRequest
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

        LLMProviderResponse response;
        try
        {
            response = await SendPingWithTransientRetryAsync(provider, pingRequest, modelId, cancellationToken);
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
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Ping timed out after {PingTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Holistic Harmonizer ping threw for model {ModelId}", modelId);
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Ping failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the ping.");
        }

        var content = response.Content ?? string.Empty;
        if (!ContainsPongJson(content))
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Model returned unexpected ping response: {preview}");
        }

        return new PlanProposalPingResult(true, stopwatch.ElapsedMilliseconds, null);
    }

    private static readonly TimeSpan TransientRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly string[] TransientErrorMarkers = { "overloaded", "rate limit", "rate_limit", "429", "503", "529" };

    /// <summary>
    /// Sends the pre-flight ping and retries once with a short backoff if the provider returns
    /// a transient capacity error (Anthropic 529 Overloaded, generic 503/429, rate-limit text).
    /// Non-transient errors propagate on the first attempt. Hard cancellations are honored.
    /// </summary>
    private async Task<LLMProviderResponse> SendPingWithTransientRetryAsync(
        ILLMProvider provider,
        LLMProviderRequest pingRequest,
        string modelId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pingCts.CancelAfter(PingTimeout);

            var response = await provider.ProcessAsync(pingRequest, pingCts.Token);
            if (response.Success || attempt == 2 || !LooksTransient(response.Error))
            {
                return response;
            }

            _logger.LogInformation(
                "Holistic Harmonizer ping transient failure for {ModelId} (attempt {Attempt}): {Error}; retrying after {Delay}s",
                modelId, attempt, response.Error, TransientRetryDelay.TotalSeconds);
            await Task.Delay(TransientRetryDelay, cancellationToken);
        }

        // Unreachable: loop returns on success or attempt==2.
        return new LLMProviderResponse { Success = false, Error = "Holistic Harmonizer ping retry loop exited unexpectedly." };
    }

    private static bool LooksTransient(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }
        var lowered = error.ToLowerInvariant();
        for (var i = 0; i < TransientErrorMarkers.Length; i++)
        {
            if (lowered.Contains(TransientErrorMarkers[i], StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<PlanProposalPingResult> CapabilityCheckAsync(string modelId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var capabilityRequest = new LLMProviderRequest
        {
            Message = CapabilityUserMessage,
            SystemPrompt = CapabilitySystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.2,
            MaxTokens = CapabilityMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
        };

        using var capabilityCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        capabilityCts.CancelAfter(CapabilityTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(capabilityRequest, capabilityCts.Token);
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
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Capability check timed out after {CapabilityTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Holistic Harmonizer capability check threw for model {ModelId}", modelId);
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Capability check failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the request.");
        }

        var content = response.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, "Model returned empty content (likely consumed token budget on internal reasoning).");
        }

        var capabilityError = ValidateCapabilityResponse(content);
        if (capabilityError is not null)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, capabilityError);
        }

        return new PlanProposalPingResult(true, stopwatch.ElapsedMilliseconds, null);
    }

    private static string? ValidateCapabilityResponse(string content)
    {
        var json = HarmonyJsonParser.ExtractJsonObject(content);
        if (json is null)
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return $"No JSON object found. Preview: {preview}";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("swaps", out var swapsEl) || swapsEl.ValueKind != JsonValueKind.Array)
            {
                return "JSON missing 'swaps' array.";
            }
            if (swapsEl.GetArrayLength() == 0)
            {
                return "Model returned empty 'swaps' array — could not propose any improvement to a clearly-imbalanced mini schedule.";
            }

            foreach (var el in swapsEl.EnumerateArray())
            {
                if (!IsValidCapabilitySwap(el, out var reason))
                {
                    return $"Invalid swap entry: {reason}";
                }
            }
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON parse failed: {ex.Message}";
        }
    }

    private static bool IsValidCapabilitySwap(JsonElement el, out string reason)
    {
        reason = string.Empty;
        if (el.ValueKind != JsonValueKind.Object)
        {
            reason = "swap is not a JSON object";
            return false;
        }
        if (!el.TryGetProperty("rowA", out var rowAEl) || !rowAEl.TryGetInt32(out var rowA) || rowA < 0 || rowA > 2)
        {
            reason = "rowA missing or out of [0..2]";
            return false;
        }
        if (!el.TryGetProperty("rowB", out var rowBEl) || !rowBEl.TryGetInt32(out var rowB) || rowB < 0 || rowB > 2)
        {
            reason = "rowB missing or out of [0..2]";
            return false;
        }
        if (rowA == rowB)
        {
            reason = "rowA equals rowB";
            return false;
        }
        if (!el.TryGetProperty("dayA", out var dayAEl) || !dayAEl.TryGetInt32(out var dayA) || dayA < 0 || dayA > 4)
        {
            reason = "dayA missing or out of [0..4]";
            return false;
        }
        if (!el.TryGetProperty("dayB", out var dayBEl) || !dayBEl.TryGetInt32(out var dayB) || dayB < 0 || dayB > 4)
        {
            reason = "dayB missing or out of [0..4]";
            return false;
        }
        if (dayA != dayB)
        {
            reason = $"dayA ({dayA}) != dayB ({dayB}) — only same-day swaps allowed";
            return false;
        }
        return true;
    }

    private static bool ContainsPongJson(string content)
    {
        var json = HarmonyJsonParser.ExtractJsonObject(content);
        if (json is null)
        {
            return false;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ping", out var pingEl)
                && pingEl.ValueKind == JsonValueKind.String
                && string.Equals(pingEl.GetString(), "pong", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task<PlanProposalResponse> ProposeAsync(PlanProposalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(request.ModelId);
        if (error is not null || model is null || provider is null)
        {
            return new PlanProposalResponse([], string.Empty, error ?? "LLM provider unavailable.");
        }

        var providerRequest = new LLMProviderRequest
        {
            Message = HarmonyPromptBuilder.BuildUserMessage(request),
            SystemPrompt = HarmonyPromptBuilder.BuildSystemPrompt(request),
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ProposalTemperature,
            MaxTokens = Math.Min(model.MaxTokens, ProposalMaxTokens),
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
            ImagePng = request.PlanPng,
        };

        using var proposalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        proposalCts.CancelAfter(ProposalTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(providerRequest, proposalCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Holistic Harmonizer LLM call timed out after {Timeout}s for model {ModelId}",
                ProposalTimeout.TotalSeconds, request.ModelId);
            return new PlanProposalResponse(
                [],
                string.Empty,
                $"LLM call timed out after {ProposalTimeout.TotalSeconds:F0}s — provider did not respond within the per-call budget.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holistic Harmonizer LLM call threw for model {ModelId}", request.ModelId);
            return new PlanProposalResponse([], string.Empty, $"LLM call failed: {ex.Message}");
        }

        if (!response.Success)
        {
            _logger.LogWarning("Holistic Harmonizer LLM provider returned error for model {ModelId}: {Error}", request.ModelId, response.Error);
            return new PlanProposalResponse([], response.Content ?? string.Empty, response.Error ?? "LLM provider error.");
        }

        var raw = response.Content ?? string.Empty;
        var parsed = HarmonyJsonParser.TryParseBatches(raw, request.MaxStepsPerBatch, request.IterationIndex, _logger, out var parseError);

        _logger.LogInformation(
            "Holistic Harmonizer LLM responded: model={Model} apiModel={ApiModel} contentLen={Len} parsedBatches={BatchCount} parsedSteps={StepCount} parseError={Err}",
            request.ModelId, model.ApiModelId, raw.Length, parsed.Count, HarmonyJsonParser.CountSteps(parsed), parseError ?? "<none>");

        return new PlanProposalResponse(parsed, raw, parseError);
    }
}
