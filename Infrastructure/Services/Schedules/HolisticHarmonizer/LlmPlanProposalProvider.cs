// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default implementation of <see cref="IPlanProposalProvider"/>. Bypasses the conversational
/// <c>ILLMService</c> (which mixes Klacks system prompts, conversation history and tool
/// calling into every request) and instead drives the underlying <see cref="ILLMProvider"/>
/// directly so the LLM receives only Holistic Harmonizer's structured prompt and replies with the JSON
/// we expect.
/// </summary>

using Klacks.Api.Domain.Constants;
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
    private static readonly TimeSpan ProposalTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PingTransientRetryDelay = TimeSpan.FromSeconds(2);

    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<LlmPlanProposalProvider> _logger;
    private readonly VisionCapabilityProbe _visionProbe;

    public LlmPlanProposalProvider(LLMProviderOrchestrator orchestrator, ILogger<LlmPlanProposalProvider> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
        _visionProbe = new VisionCapabilityProbe(logger);
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

        var pingRequest = HolisticHarmonizerProbeRequests.Ping(model);

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

        var verdict = PlanProposalPingEvaluator.Evaluate(response);
        if (verdict.OutputBudgetSpentOnThinking)
        {
            _logger.LogWarning(
                "Holistic Harmonizer ping for {ModelId} was cut off after {ReasoningTokens} reasoning tokens; the model is reachable, continuing",
                modelId, response.ReasoningTokens);
        }

        return new PlanProposalPingResult(verdict.IsHealthy, stopwatch.ElapsedMilliseconds, verdict.Error);
    }

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
            if (response.Success || attempt == 2 || !TransientProviderErrorDetector.IsTransient(response.Error))
            {
                return response;
            }

            _logger.LogInformation(
                "Holistic Harmonizer ping transient failure for {ModelId} (attempt {Attempt}): {Error}; retrying after {Delay}s",
                modelId, attempt, response.Error, PingTransientRetryDelay.TotalSeconds);
            await Task.Delay(PingTransientRetryDelay, cancellationToken);
        }

        // Unreachable: loop returns on success or attempt==2.
        return new LLMProviderResponse { Success = false, Error = "Holistic Harmonizer ping retry loop exited unexpectedly." };
    }

    public async Task<PlanProposalPingResult> CapabilityCheckAsync(string modelId, CancellationToken cancellationToken)
    {
        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            return new PlanProposalPingResult(false, 0, error ?? "LLM provider unavailable.");
        }

        return await _visionProbe.RunAsync(model, provider, cancellationToken);
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
            ThinkingBudgetTokens = ThinkingBudgetConstants.Disabled,
            SupportedParameters = model.SupportedParameters,
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
        var parsed = HarmonyJsonParser.TryParseBatches(
            raw, request.MaxStepsPerBatch, request.IterationIndex, _logger, out var parseError, out var explicitlyEmpty);

        _logger.LogInformation(
            "Holistic Harmonizer LLM responded: model={Model} apiModel={ApiModel} contentLen={Len} parsedBatches={BatchCount} parsedSteps={StepCount} parseError={Err} explicitlyEmpty={Flag}",
            request.ModelId, model.ApiModelId, raw.Length, parsed.Count, HarmonyJsonParser.CountSteps(parsed), parseError ?? "<none>", explicitlyEmpty);

        return new PlanProposalResponse(
            parsed, raw, parseError, LlmSignaledSatisfied: parseError is null && explicitlyEmpty);
    }
}
