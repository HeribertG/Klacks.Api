// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// HttpClient-based client for the region-package endpoints of the Klacks Marketplace. Uses the
/// same base URL configuration as <see cref="MarketplaceClientService"/>. A 404 on the latest-version
/// lookup is reported as a not-found result (a country without a published package is a normal
/// state); every real failure (other non-success status, invalid payload, network error) is logged
/// and surfaced as a failed result or null instead of an exception.
/// </summary>
/// <param name="httpClient">HttpClient instance configured by the typed-client registration</param>
/// <param name="configuration">App configuration providing the marketplace base URL</param>
/// <param name="logger">Logger instance for diagnostic output</param>
using System.Net;
using System.Text.Json;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionPackageMarketplaceClient : IRegionPackageMarketplaceClient
{
    public const string MarketplaceUrlConfigKey = "LanguagePlugins:MarketplaceUrl";

    private const string RegionsPathSegment = "api/regions";
    private const string DownloadPathSegment = "download";
    private const string DownloadQuery = "industry=all&artifact=profileJson";
    private const long MaxResponseContentBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<RegionPackageMarketplaceClient> _logger;

    public RegionPackageMarketplaceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RegionPackageMarketplaceClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = RequestTimeout;
        _httpClient.MaxResponseContentBufferSize = MaxResponseContentBytes;
        _baseUrl = configuration.GetValue<string>(MarketplaceUrlConfigKey) ?? string.Empty;
        _logger = logger;
    }

    public async Task<MarketplaceRegionPackageLookup> GetLatestAsync(string countryCode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_baseUrl}/{RegionsPathSegment}/{Uri.EscapeDataString(countryCode)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "Marketplace has no published region package for '{Country}'",
                    countryCode.ForLog());
                return MarketplaceRegionPackageLookup.PackageNotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Marketplace region package lookup for '{Country}' failed with status {StatusCode}",
                    countryCode.ForLog(),
                    response.StatusCode);
                return MarketplaceRegionPackageLookup.Failed();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var info = JsonSerializer.Deserialize<MarketplaceRegionPackageInfo>(json, JsonOptions);
            if (info == null || string.IsNullOrWhiteSpace(info.Version))
            {
                _logger.LogWarning(
                    "Marketplace region package response for '{Country}' contains no version",
                    countryCode.ForLog());
                return MarketplaceRegionPackageLookup.Failed();
            }

            return MarketplaceRegionPackageLookup.Found(info);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to look up marketplace region package '{Country}'", countryCode.ForLog());
            return MarketplaceRegionPackageLookup.Failed();
        }
    }

    public async Task<string?> DownloadProfileJsonAsync(string countryCode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_baseUrl}/{RegionsPathSegment}/{Uri.EscapeDataString(countryCode)}/{DownloadPathSegment}?{DownloadQuery}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Marketplace region package download for '{Country}' failed with status {StatusCode}",
                    countryCode.ForLog(),
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download marketplace region package '{Country}'", countryCode.ForLog());
            return null;
        }
    }
}
