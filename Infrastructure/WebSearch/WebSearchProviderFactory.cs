// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the configured web search provider from the settings store, decrypting the stored API key
/// before handing it to the provider.
/// </summary>
/// <param name="settingsRepository">Reads the provider name and the stored API key setting</param>
/// <param name="httpClientFactory">Supplies the HTTP client the provider talks to its service with</param>
/// <param name="encryptionService">Turns the stored API key back into plain text before it is used</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.WebSearch;

public class WebSearchProviderFactory : IWebSearchProviderFactory
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsEncryptionService _encryptionService;

    public WebSearchProviderFactory(
        ISettingsRepository settingsRepository,
        IHttpClientFactory httpClientFactory,
        ISettingsEncryptionService encryptionService)
    {
        _settingsRepository = settingsRepository;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
    }

    public async Task<IWebSearchProvider?> CreateAsync(CancellationToken ct = default)
    {
        var providerSetting = await _settingsRepository.GetSetting(Settings.WEB_SEARCH_PROVIDER);
        var apiKeySetting = await _settingsRepository.GetSetting(Settings.WEB_SEARCH_API_KEY);

        var provider = providerSetting?.Value;
        var apiKey = apiKeySetting == null || string.IsNullOrEmpty(apiKeySetting.Value)
            ? null
            : _encryptionService.ProcessForReading(apiKeySetting.Type, apiKeySetting.Value);

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(apiKey))
            return null;

        return provider.ToLowerInvariant() switch
        {
            "serper" => new SerperWebSearchProvider(apiKey, _httpClientFactory),
            "tavily" => new TavilyWebSearchProvider(apiKey, _httpClientFactory),
            _ => null
        };
    }
}
