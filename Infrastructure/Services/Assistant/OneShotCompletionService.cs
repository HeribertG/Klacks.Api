// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pipeline-free single LLM completion: resolves the model (the configured default when no model id is
/// given) through LLMProviderOrchestrator, sends one tool-free request at temperature 0 with the model's
/// own token budget and retries transient provider errors via TransientProviderRetry — the same policy
/// the chat pipeline uses. No conversation, history, memory, recipe engine, usage recording or
/// background tasks are involved (like RecipeSlotExtractor/GroupPlaceClassifier, which call providers
/// directly and record no usage either). Model resolution errors, provider errors and exceptions all
/// come back as a failed result; only cancellation is rethrown.
/// </summary>
/// <param name="providerOrchestrator">Resolves model and provider (scoped, caches per request)</param>
/// <param name="logger">Logs failures and retried transient errors</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class OneShotCompletionService : IOneShotCompletionService
{
    private const double CompletionTemperature = 0.0;
    private const string EmptyProviderErrorMessage = "Provider returned no error details.";

    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly ILogger<OneShotCompletionService> _logger;

    public OneShotCompletionService(
        LLMProviderOrchestrator providerOrchestrator,
        ILogger<OneShotCompletionService> logger)
    {
        _providerOrchestrator = providerOrchestrator;
        _logger = logger;
    }

    public async Task<OneShotCompletionResult> CompleteAsync(
        string systemPrompt,
        string userMessage,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(modelId);
            if (model == null || provider == null)
            {
                var reason = error ?? EmptyProviderErrorMessage;
                _logger.LogWarning("One-shot completion could not resolve a model/provider: {Error}", reason);
                return OneShotCompletionResult.Failed(reason);
            }

            var request = new LLMProviderRequest
            {
                SystemPrompt = systemPrompt,
                Message = userMessage,
                ModelId = model.ApiModelId,
                Temperature = CompletionTemperature,
                MaxTokens = model.MaxTokens,
                SupportedParameters = model.SupportedParameters,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                ConversationHistory = [],
                AvailableFunctions = []
            };

            var response = await TransientProviderRetry.ProcessAsync(provider, request, _logger, cancellationToken);
            if (!response.Success)
            {
                var reason = string.IsNullOrWhiteSpace(response.Error) ? EmptyProviderErrorMessage : response.Error;
                _logger.LogWarning(
                    "One-shot completion failed on model {ModelId}: {Error}", model.ModelId, reason);
                return OneShotCompletionResult.Failed(reason);
            }

            return OneShotCompletionResult.Succeeded(response.Content ?? string.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "One-shot completion threw");
            return OneShotCompletionResult.Failed(ex.Message);
        }
    }
}
