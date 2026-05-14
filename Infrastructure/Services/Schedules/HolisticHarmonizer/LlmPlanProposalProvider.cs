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
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
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

    private const int CapabilityMaxTokens = 200;
    private static readonly TimeSpan CapabilityTimeout = TimeSpan.FromSeconds(90);
    private const string CapabilitySystemPrompt =
        "You are a deterministic vision-capability verifier for the Klacks Holistic Harmonizer (Wizard 3).\n" +
        "Wizard 3 mutates a bitmap-rendered schedule, so the host accepts only models that genuinely process attached images.\n" +
        "You receive a small PNG containing exactly one short alphabetic token painted in large bold black letters on a yellow box.\n" +
        "Reply with ONE JSON object and nothing else: {\"token\":\"...\"}.\n" +
        "No prose, no markdown, no code fences, no commentary.\n" +
        "If you cannot see or process the image, reply with {\"token\":\"\"}.";
    private const string CapabilityUserMessage =
        "Read the token printed in the attached image and reply with the JSON object only.";

    private static readonly char[] CapabilityTokenAlphabet =
        { 'E', 'F', 'H', 'K', 'L', 'N', 'P', 'T', 'X', 'Z' };
    private const int CapabilityTokenLength = 3;

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

        var expectedToken = GenerateCapabilityToken();
        byte[] capabilityPng;
        try
        {
            capabilityPng = VisionCapabilityPngRenderer.Render(expectedToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Holistic Harmonizer vision capability PNG generation failed for model {ModelId}", modelId);
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Capability PNG generation failed: {ex.Message}");
        }

        var capabilityRequest = new LLMProviderRequest
        {
            Message = CapabilityUserMessage,
            SystemPrompt = CapabilitySystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.0,
            MaxTokens = CapabilityMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
            ImagePng = capabilityPng,
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

        var capabilityError = ValidateVisionResponse(content, expectedToken);
        if (capabilityError is not null)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, capabilityError);
        }

        return new PlanProposalPingResult(true, stopwatch.ElapsedMilliseconds, null);
    }

    private static string GenerateCapabilityToken()
    {
        Span<char> buffer = stackalloc char[CapabilityTokenLength];
        Span<int> chosen = stackalloc int[CapabilityTokenLength];
        for (var i = 0; i < CapabilityTokenLength; i++)
        {
            int index;
            var unique = false;
            while (!unique)
            {
                index = Random.Shared.Next(CapabilityTokenAlphabet.Length);
                unique = true;
                for (var j = 0; j < i; j++)
                {
                    if (chosen[j] == index)
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique)
                {
                    chosen[i] = index;
                    buffer[i] = CapabilityTokenAlphabet[index];
                }
            }
        }
        return new string(buffer);
    }

    private static string? ValidateVisionResponse(string content, string expectedToken)
    {
        var json = HarmonyJsonParser.ExtractJsonObject(content);
        if (json is null)
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return $"No JSON object found in response. Preview: {preview}";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("token", out var tokenEl) || tokenEl.ValueKind != JsonValueKind.String)
            {
                return "JSON missing string 'token' field — model likely cannot process attached bitmaps.";
            }
            var actualToken = tokenEl.GetString();
            if (string.IsNullOrWhiteSpace(actualToken))
            {
                return "Model returned empty 'token' — bitmap processing not supported.";
            }
            var normalised = NormaliseToken(actualToken);
            if (!string.Equals(normalised, expectedToken, StringComparison.Ordinal))
            {
                return $"Model read token '{actualToken}' but the image showed '{expectedToken}'. Model does not reliably process bitmaps required by Wizard 3.";
            }
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON parse failed: {ex.Message}";
        }
    }

    private static string NormaliseToken(string raw)
    {
        Span<char> buffer = stackalloc char[raw.Length];
        var length = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (char.IsLetterOrDigit(c))
            {
                buffer[length++] = char.ToUpperInvariant(c);
            }
        }
        return new string(buffer[..length]);
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
