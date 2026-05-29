// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fetches and parses the per-channel update manifest from the deployment-configured base URL.
/// Returns null (never throws) when no URL is configured, the request fails or the JSON is invalid,
/// so the read path degrades gracefully to "no update information". Signature verification is NOT
/// done here — it belongs to the out-of-process updater before applying an artifact.
/// </summary>
/// <param name="httpClientFactory">Factory for the named manifest HTTP client</param>
/// <param name="options">Deployment trust configuration (manifest base URL)</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Update;

public class UpdateManifestReader : IUpdateManifestReader
{
    public const string HttpClientName = "UpdateManifest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateTrustOptions _options;
    private readonly ILogger<UpdateManifestReader> _logger;

    public UpdateManifestReader(
        IHttpClientFactory httpClientFactory,
        IOptions<UpdateTrustOptions> options,
        ILogger<UpdateManifestReader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UpdateManifest?> GetManifestAsync(UpdateChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ManifestBaseUrl))
        {
            _logger.LogInformation("Update manifest base URL is not configured. Skipping manifest fetch.");
            return null;
        }

        var url = $"{_options.ManifestBaseUrl.TrimEnd('/')}/{channel.ToString().ToLowerInvariant()}.json";

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update manifest fetch from {Url} returned {StatusCode}.", url, (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch or parse update manifest from {Url}.", url);
            return null;
        }
    }
}
