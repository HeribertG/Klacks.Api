// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using AppSettings = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMProviderOrchestrator
{
    private readonly ILogger<LLMProviderOrchestrator> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _repository;

    // A chat turn resolves the same model twice — once early to size the tool budget, once inside
    // LLMService — costing up to three DB roundtrips each. This service is scoped, so caching the
    // successful resolution per request removes the second lookup while keeping both call sites on
    // the identical model, which is what pins the turn to one model.
    private readonly Dictionary<string, (LLMModel Model, ILLMProvider Provider)> _resolved = new();

    public LLMProviderOrchestrator(
        ILogger<LLMProviderOrchestrator> logger,
        ILLMProviderFactory providerFactory,
        ILLMRepository repository)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _repository = repository;
    }

    public async Task<(LLMModel? model, ILLMProvider? provider, string? error)> GetModelAndProviderAsync(string? modelId)
    {
        var cacheKey = modelId ?? string.Empty;
        if (_resolved.TryGetValue(cacheKey, out var hit))
        {
            return (hit.Model, hit.Provider, null);
        }

        try
        {
            var effectiveModelId = string.IsNullOrWhiteSpace(modelId) ? await GetDefaultModelIdAsync() : modelId;
            var model = await _repository.GetModelByIdAsync(effectiveModelId);

            if (model == null || !model.IsEnabled)
            {
                _logger.LogWarning("Model {ModelId} not found or not enabled", effectiveModelId.ForLog());
                return (null, null, "The selected model is not available.");
            }

            var provider = await _providerFactory.GetProviderForModelAsync(effectiveModelId);
            if (provider == null)
            {
                _logger.LogWarning("Provider for model {ModelId} not found", effectiveModelId.ForLog());
                return (null, null, "The provider for the selected model is not available.");
            }

            // Also key it by the resolved id: the early call passes null (or the request's id) and pins
            // the result, so the second call arrives with the concrete id and would otherwise miss.
            _resolved[cacheKey] = (model, provider);
            _resolved[model.ModelId] = (model, provider);
            return (model, provider, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model and provider");
            return (null, null, "An error occurred while loading the model.");
        }
    }

    private async Task<string> GetDefaultModelIdAsync()
    {
        var defaultModel = await _repository.GetDefaultModelAsync();
        return defaultModel?.ModelId ?? AppSettings.LLM_FALLBACK_MODEL_ID;
    }
}
