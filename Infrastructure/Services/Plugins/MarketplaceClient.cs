// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// HTTP client for the Klacks Marketplace REST API to search and download feature plugins.
/// </summary>
/// <param name="httpClient">Configured HttpClient for marketplace API calls</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Text.Json;
using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces.Plugins;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public class MarketplaceClient : IMarketplaceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketplaceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MarketplaceClient(HttpClient httpClient, ILogger<MarketplaceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<MarketplacePluginInfo>> SearchPluginsAsync(string? search, string? category)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        }
        queryParams.Add("pageSize=100");

        var url = $"api/plugins?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MarketplaceSearchResult>(json, JsonOptions);
            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search marketplace plugins");
            return [];
        }
    }

    public async Task<byte[]> DownloadPluginAsync(string name)
    {
        var url = $"api/plugins/{Uri.EscapeDataString(name)}/download";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }

    private class MarketplaceSearchResult
    {
        public List<MarketplacePluginInfo> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
