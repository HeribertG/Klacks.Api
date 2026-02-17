using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using AppSettings = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMProviderOrchestrator
{
    private readonly ILogger<LLMProviderOrchestrator> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _repository;

    public LLMProviderOrchestrator(
        ILogger<LLMProviderOrchestrator> logger,
        ILLMProviderFactory providerFactory,
        ILLMRepository repository)
    {
        this._logger = logger;
        _providerFactory = providerFactory;
        _repository = repository;
    }

    public async Task<(LLMModel? model, ILLMProvider? provider, string? error)> GetModelAndProviderAsync(string? modelId)
    {
        try
        {
            var effectiveModelId = string.IsNullOrWhiteSpace(modelId) ? await GetDefaultModelIdAsync() : modelId;
            var model = await _repository.GetModelByIdAsync(effectiveModelId);

            if (model == null || !model.IsEnabled)
            {
                _logger.LogWarning("Model {ModelId} not found or not enabled", effectiveModelId);
                return (null, null, "The selected model is not available.");
            }

            var provider = await _providerFactory.GetProviderForModelAsync(effectiveModelId);
            if (provider == null)
            {
                _logger.LogWarning("Provider for model {ModelId} not found", effectiveModelId);
                return (null, null, "The provider for the selected model is not available.");
            }

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
