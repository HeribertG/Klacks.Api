using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.LLM.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Domain.Services.LLM;

public interface ILLMProviderFactory
{
    Task<ILLMProvider?> GetProviderAsync(string providerId);
    Task<ILLMProvider?> GetProviderForModelAsync(string modelId);
    Task<List<ILLMProvider>> GetEnabledProvidersAsync();
}

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILLMRepository _repository;
    private readonly ILogger<LLMProviderFactory> _logger;

    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        ILLMRepository repository,
        ILogger<LLMProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        _logger = logger;
    }

    public async Task<ILLMProvider?> GetProviderAsync(string providerId)
    {
        var providerConfig = await _repository.GetProviderByIdAsync(providerId);
        if (providerConfig == null || !providerConfig.IsEnabled)
            return null;

        return providerId.ToLower() switch
        {
            "openai" => _serviceProvider.GetService<OpenAIProvider>(),
            "anthropic" => _serviceProvider.GetService<AnthropicProvider>(),
            "google" => _serviceProvider.GetService<GeminiProvider>(),
            _ => null
        };
    }

    public async Task<ILLMProvider?> GetProviderForModelAsync(string modelId)
    {
        var model = await _repository.GetModelByIdAsync(modelId);
        if (model == null || !model.IsEnabled)
            return null;

        return await GetProviderAsync(model.Provider.ProviderId);
    }

    public async Task<List<ILLMProvider>> GetEnabledProvidersAsync()
    {
        var providers = await _repository.GetProvidersAsync();
        var enabledProviders = new List<ILLMProvider>();

        foreach (var provider in providers.Where(p => p.IsEnabled))
        {
            var concreteProvider = await GetProviderAsync(provider.ProviderId);
            if (concreteProvider != null)
            {
                enabledProviders.Add(concreteProvider);
            }
        }

        return enabledProviders.OrderBy(p => providers.First(cfg => cfg.ProviderId == p.ProviderId).Priority).ToList();
    }
}