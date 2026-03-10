// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// HttpClient-basierter Service für die Kommunikation mit dem Klacks Marketplace.
/// </summary>
/// <param name="baseUrl">Base-URL des Marketplace-Servers aus der Konfiguration</param>
/// <param name="apiKey">API-Key für Upload-Authentifizierung</param>
using System.Net.Http.Json;
using System.Text.Json;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class MarketplaceClientService : IMarketplaceClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger<MarketplaceClientService> _logger;

    public MarketplaceClientService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MarketplaceClientService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _baseUrl = configuration.GetValue<string>("LanguagePlugins:MarketplaceUrl") ?? string.Empty;
        _apiKey = configuration.GetValue<string>("LanguagePlugins:MarketplaceApiKey") ?? string.Empty;
        _logger = logger;
    }

    public async Task<MarketplaceSearchResultDto?> SearchPackagesAsync(string? search, int page, int pageSize)
    {
        try
        {
            var url = $"{_baseUrl}/api/packages?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Marketplace search failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MarketplaceSearchResultDto>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search marketplace packages");
            return null;
        }
    }

    public async Task<MarketplacePackageDto?> GetPackageAsync(string code)
    {
        try
        {
            var url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(code)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Marketplace get package '{Code}' failed with status {StatusCode}", code, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MarketplacePackageDto>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get marketplace package '{Code}'", code);
            return null;
        }
    }

    public async Task<byte[]?> DownloadPackageAsync(string code)
    {
        try
        {
            var url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(code)}/download";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Marketplace download package '{Code}' failed with status {StatusCode}", code, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download marketplace package '{Code}'", code);
            return null;
        }
    }

    public async Task<bool> UploadPackageAsync(string manifestJson, string translationsJson, string? docsJson, string? countriesJson, string? statesJson, string? calendarRulesJson)
    {
        try
        {
            var url = $"{_baseUrl}/api/packages";

            using var manifestDoc = JsonDocument.Parse(manifestJson);
            var manifest = manifestDoc.RootElement;

            var payload = new
            {
                code = manifest.GetProperty("code").GetString(),
                name = manifest.GetProperty("name").GetString(),
                displayName = manifest.GetProperty("displayName").GetString(),
                speechLocale = manifest.GetProperty("speechLocale").GetString(),
                version = manifest.GetProperty("version").GetString(),
                coverage = manifest.TryGetProperty("coverage", out var cov) ? cov.GetDouble() : 0.0,
                description = manifest.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty,
                minKlacksVersion = manifest.TryGetProperty("minKlacksVersion", out var mkv) ? mkv.GetString() : "1.0.0",
                manifestJson,
                translationsJson,
                docsJson,
                countriesJson,
                statesJson,
                calendarRulesJson
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Marketplace upload failed with status {StatusCode}: {Body}", response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upload package to marketplace");
            return false;
        }
    }
}
