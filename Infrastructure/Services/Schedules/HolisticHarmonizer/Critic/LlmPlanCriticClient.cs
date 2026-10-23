// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Default <see cref="IPlanCriticClient"/>: drives the configured <see cref="ILLMProvider"/> directly, like
/// <see cref="LlmPlanProposalProvider"/>, so the model receives only the critic prompt, the anonymized plan image and
/// text grid - no Klacks system prompt, no conversation history, no tools. Thinking is switched off for the same
/// reason as for proposals (a reasoning model otherwise spends the output budget on reasoning). Logs carry counts and
/// lengths only, never the answer text.
/// </summary>
public sealed class LlmPlanCriticClient : IPlanCriticClient
{
    private const double CriticTemperature = 0.1;
    private const int CriticMaxTokens = 4000;
    private static readonly TimeSpan CriticTimeout = TimeSpan.FromSeconds(60);

    private const string ProviderUnavailableError = "LLM provider unavailable.";
    private const string ProviderError = "LLM provider error.";
    private const string TimeoutError = "Critic call timed out after {0:F0}s.";
    private const string CallFailedError = "Critic call failed: {0}";

    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<LlmPlanCriticClient> _logger;

    public LlmPlanCriticClient(LLMProviderOrchestrator orchestrator, ILogger<LlmPlanCriticClient> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<PlanCriticClientResponse> ReviewAsync(PlanCriticRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(request.ModelId);
        if (error is not null || model is null || provider is null)
        {
            return PlanCriticClientResponse.Failure(error ?? ProviderUnavailableError);
        }

        var providerRequest = new LLMProviderRequest
        {
            Message = PlanCriticPromptBuilder.BuildUserMessage(request),
            SystemPrompt = PlanCriticPromptBuilder.BuildSystemPrompt(request),
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = CriticTemperature,
            MaxTokens = Math.Min(model.MaxTokens, CriticMaxTokens),
            ThinkingBudgetTokens = ThinkingBudgetConstants.Disabled,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
            ImagePng = request.PlanPng,
        };

        using var callCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        callCts.CancelAfter(CriticTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(providerRequest, callCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return PlanCriticClientResponse.Failure(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, TimeoutError, CriticTimeout.TotalSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wizard 3 critic call threw for model {ModelId}", request.ModelId);
            return PlanCriticClientResponse.Failure(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, CallFailedError, ex.Message));
        }

        if (!response.Success)
        {
            return PlanCriticClientResponse.Failure(response.Error ?? ProviderError);
        }

        var answer = response.Content ?? string.Empty;
        var findings = PlanCriticJsonParser.TryParse(answer, out var parseError);
        _logger.LogInformation(
            "Wizard 3 critic responded: model={ModelId} apiModel={ApiModel} contentLen={Len} parsedFindings={Count} parseError={HasError}",
            request.ModelId, model.ApiModelId, answer.Length, findings.Count, parseError is not null);

        return parseError is null
            ? new PlanCriticClientResponse(findings, null)
            : PlanCriticClientResponse.Failure(parseError);
    }
}
