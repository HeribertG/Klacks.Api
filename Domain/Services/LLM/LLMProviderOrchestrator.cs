using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Services.LLM.Providers;

namespace Klacks.Api.Domain.Services.LLM;

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
        _logger = logger;
        _providerFactory = providerFactory;
        _repository = repository;
    }

    public async Task<(LLMModel? model, ILLMProvider? provider, string? error)> GetModelAndProviderAsync(string? modelId)
    {
        try
        {
            var effectiveModelId = modelId ?? await GetDefaultModelIdAsync();
            var model = await _repository.GetModelByIdAsync(effectiveModelId);

            if (model == null || !model.IsEnabled)
            {
                _logger.LogWarning("Model {ModelId} not found or not enabled", effectiveModelId);
                return (null, null, "Das gewählte Modell ist nicht verfügbar.");
            }

            var provider = await _providerFactory.GetProviderForModelAsync(effectiveModelId);
            if (provider == null)
            {
                _logger.LogWarning("Provider for model {ModelId} not found", effectiveModelId);
                return (null, null, "Der Provider für das gewählte Modell ist nicht verfügbar.");
            }

            return (model, provider, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model and provider");
            return (null, null, "Ein Fehler ist beim Laden des Modells aufgetreten.");
        }
    }

    private async Task<string> GetDefaultModelIdAsync()
    {
        var defaultModel = await _repository.GetDefaultModelAsync();
        return defaultModel?.ModelId ?? "gpt-5";
    }
}
