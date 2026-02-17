using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI;

namespace Klacks.Api.Infrastructure.Services.Assistant;

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
        this._logger = logger;
    }

    public async Task<ILLMProvider?> GetProviderAsync(string providerId)
    {
        var providerConfig = await _repository.GetProviderByIdAsync(providerId);
        if (providerConfig == null || !providerConfig.IsEnabled)
        {
            return null;
        }

        ILLMProvider? provider = providerId.ToLower() switch
        {
            "openai" => _serviceProvider.GetService<OpenAIProvider>(),
            "anthropic" => _serviceProvider.GetService<AnthropicProvider>(),
            "google" => _serviceProvider.GetService<GeminiProvider>(),
            "azure" => _serviceProvider.GetService<AzureOpenAIProvider>(),
            "mistral" => _serviceProvider.GetService<MistralProvider>(),
            "cohere" => _serviceProvider.GetService<CohereProvider>(),
            "deepseek" => _serviceProvider.GetService<DeepSeekProvider>(),
            _ => _serviceProvider.GetService<GenericOpenAICompatibleProvider>()
        };

        if (provider != null)
        {
            provider.Configure(providerConfig);
        }

        return provider;
    }

    public async Task<ILLMProvider?> GetProviderForModelAsync(string modelId)
    {
        var model = await _repository.GetModelByIdAsync(modelId);
        if (model == null || !model.IsEnabled)
        {
            return null;
        }

        return await GetProviderAsync(model.ProviderId);
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