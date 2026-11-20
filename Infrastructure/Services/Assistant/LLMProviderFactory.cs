// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Playground;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Pseudonymization;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILLMRepository _repository;
    private readonly ILogger<LLMProviderFactory> _logger;
    private readonly IPlaygroundMode _playgroundMode;
    private readonly IPlaygroundTokenBudget _playgroundBudget;
    private readonly ISettingsReader _settingsReader;
    private readonly IPseudonymMapSource _pseudonymMapSource;
    private readonly ICompanyClock _companyClock;

    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        ILLMRepository repository,
        ILogger<LLMProviderFactory> logger,
        IPlaygroundMode playgroundMode,
        IPlaygroundTokenBudget playgroundBudget,
        ISettingsReader settingsReader,
        IPseudonymMapSource pseudonymMapSource,
        ICompanyClock companyClock)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        this._logger = logger;
        _playgroundMode = playgroundMode;
        _playgroundBudget = playgroundBudget;
        _settingsReader = settingsReader;
        _pseudonymMapSource = pseudonymMapSource;
        _companyClock = companyClock;
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
            "deepseek" => _serviceProvider.GetService<DeepSeekProvider>(),
            _ => _serviceProvider.GetService<GenericOpenAICompatibleProvider>()
        };

        if (provider == null)
        {
            return null;
        }

        provider.Configure(providerConfig);

        if (await IsPseudonymizationEnabledAsync())
        {
            // Only the generic OpenAI-compatible client really sends to its BaseUrl; the named cloud clients (Gemini
            // for one) ignore it for most requests, so a local-looking BaseUrl on them must never exempt the call.
            var localCandidateUrl = provider is GenericOpenAICompatibleProvider ? providerConfig.BaseUrl : null;
            provider = new PseudonymizingLLMProvider(
                provider, localCandidateUrl, _pseudonymMapSource, _companyClock, _logger);
        }

        // The single wrap point: GetProviderForModelAsync and GetEnabledProvidersAsync both resolve through
        // here, so wrapping in the other two getters as well would charge every call twice.
        return _playgroundMode.IsActive
            ? new PlaygroundBudgetedLLMProvider(provider, _playgroundBudget)
            : provider;
    }

    private async Task<bool> IsPseudonymizationEnabledAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.LlmPseudonymizationEnabled);
        return !(bool.TryParse(setting?.Value?.Trim(), out var enabled) && !enabled);
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