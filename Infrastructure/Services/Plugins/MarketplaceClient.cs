// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// HTTP client for the Klacks Marketplace REST API to search and download feature plugins. Plugin
/// downloads go through <see cref="MarketplacePackageDownloader"/> (byte cap + vendor signature check).
/// </summary>
/// <param name="httpClient">Configured HttpClient for marketplace API calls</param>
/// <param name="packageDownloader">Size-capped, signature-verifying package download shared with language packs</param>
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Infrastructure.Services.Settings;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public class MarketplaceClient : IMarketplaceClient
{
    private readonly HttpClient _httpClient;
    private readonly MarketplacePackageDownloader _packageDownloader;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MarketplaceClient(HttpClient httpClient, MarketplacePackageDownloader packageDownloader)
    {
        _httpClient = httpClient;
        _packageDownloader = packageDownloader;
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

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MarketplaceSearchResult>(json, JsonOptions);
        return result?.Items ?? [];
    }

    public Task<MarketplacePackageDownload> DownloadPluginAsync(string name, CancellationToken cancellationToken)
    {
        var url = $"api/plugins/{Uri.EscapeDataString(name)}/download";

        return _packageDownloader.DownloadAsync(
            _httpClient,
            url,
            MarketplaceArtifactTypes.Plugin,
            name,
            MarketplacePackageLimits.MaxPluginDownloadBytes,
            cancellationToken);
    }

    private class MarketplaceSearchResult
    {
        public List<MarketplacePluginInfo> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
