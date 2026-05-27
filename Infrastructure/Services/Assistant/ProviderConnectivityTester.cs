// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tests whether an LLM provider base URL is reachable by calling its "models" endpoint.
/// Uses a fresh HttpClient per call (no shared mutable state) so it is safe to run in parallel.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class ProviderConnectivityTester : IProviderConnectivityTester
{
    private const string ModelsEndpoint = "models";
    private const string HttpClientName = "ProviderConnectivityTester";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderConnectivityTester> _logger;

    public ProviderConnectivityTester(
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderConnectivityTester> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProviderConnectivityStatus> TestAsync(
        string baseUrl,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        var normalized = ProviderUrlHelper.EnsureTrailingSlash(baseUrl);
        if (normalized.Length == 0 ||
            !Uri.TryCreate(normalized, UriKind.Absolute, out var baseUri))
        {
            return ProviderConnectivityStatus.Unreachable;
        }

        var endpoint = new Uri(baseUri, ModelsEndpoint);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await client.SendAsync(request, timeoutCts.Token);

            return ClassifyStatus((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Connectivity test failed for {BaseUrl}", normalized);
            return ProviderConnectivityStatus.Unreachable;
        }
    }

    private static ProviderConnectivityStatus ClassifyStatus(int statusCode)
    {
        if (statusCode is 401 or 403)
        {
            return ProviderConnectivityStatus.ReachableNeedsKey;
        }

        if (statusCode >= 200 && statusCode < 300)
        {
            return ProviderConnectivityStatus.Reachable;
        }

        return ProviderConnectivityStatus.Unreachable;
    }
}
