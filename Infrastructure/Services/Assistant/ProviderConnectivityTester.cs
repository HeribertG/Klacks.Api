// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tests whether an LLM provider base URL is reachable by calling its "models" endpoint.
/// Uses a fresh HttpClient per call (no shared mutable state) so it is safe to run in parallel.
/// Rejects any non-http(s) scheme upfront. Requests to private, loopback or link-local addresses
/// (including addresses reached only via an HTTP redirect) are refused by the named HttpClient's
/// <see cref="PrivateNetworkBlockingConnectCallback"/>, so a malicious/compromised admin cannot
/// use this admin-only endpoint for SSRF against internal infrastructure or cloud metadata
/// services.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Security;

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

        if (!IsAllowedScheme(baseUri.Scheme))
        {
            _logger.LogWarning(
                "Rejected connectivity test for {BaseUrl}: scheme '{Scheme}' is not allowed, only http/https are permitted.",
                normalized, baseUri.Scheme);
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
            if (IsBlockedByPrivateNetworkGuard(ex))
            {
                _logger.LogWarning(ex, "Rejected connectivity test for {BaseUrl}: blocked by the private-network SSRF guard.", normalized);
            }
            else
            {
                _logger.LogDebug(ex, "Connectivity test failed for {BaseUrl}", normalized);
            }

            return ProviderConnectivityStatus.Unreachable;
        }
    }

    private static bool IsAllowedScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsBlockedByPrivateNetworkGuard(Exception ex) =>
        ex is PrivateNetworkAccessBlockedException || ex.InnerException is PrivateNetworkAccessBlockedException;

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
