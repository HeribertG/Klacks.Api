// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.WebSearch;

public class WebSearchProviderFactory
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebSearchProviderFactory(
        ISettingsRepository settingsRepository,
        IHttpClientFactory httpClientFactory)
    {
        _settingsRepository = settingsRepository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IWebSearchProvider?> CreateAsync(CancellationToken ct = default)
    {
        var providerSetting = await _settingsRepository.GetSetting(Settings.WEB_SEARCH_PROVIDER);
        var apiKeySetting = await _settingsRepository.GetSetting(Settings.WEB_SEARCH_API_KEY);

        var provider = providerSetting?.Value;
        var apiKey = apiKeySetting?.Value;

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
