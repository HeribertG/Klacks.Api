// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Logs the effective geo service configuration once at startup, one line per service with its host only
/// (never paths or keys). A disabled service or an active NoOutboundTraffic switch is logged as a warning,
/// an unusable configuration as an error, because production only logs from warning upwards.
/// </summary>
/// <param name="policy">Effective geo service configuration</param>
/// <param name="logger">Startup logger</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Services.GeoServices;

namespace Klacks.Api.Infrastructure.Services;

public class GeoServicesStartupReporter : IHostedService
{
    private static readonly GeoServiceKind[] ReportedEndpointKinds =
    [
        GeoServiceKind.Geocoding,
        GeoServiceKind.Routing,
        GeoServiceKind.OpenRouteService
    ];

    private readonly IGeoServicesPolicy _policy;
    private readonly ILogger<GeoServicesStartupReporter> _logger;

    public GeoServicesStartupReporter(IGeoServicesPolicy policy, ILogger<GeoServicesStartupReporter> logger)
    {
        _policy = policy;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_policy.NoOutboundTraffic)
        {
            _logger.LogWarning("Geo services: NoOutboundTraffic is active, public geo services are blocked");
        }

        foreach (var kind in ReportedEndpointKinds)
        {
            var endpoint = _policy.Get(kind);
            Report(kind, endpoint.IsEnabled, endpoint.BaseUrl, endpoint.DisabledReason);
        }

        var mapTiles = _policy.MapTiles;
        Report(GeoServiceKind.MapTiles, mapTiles.IsEnabled, mapTiles.UrlTemplate, mapTiles.DisabledReason);

        return Task.CompletedTask;
    }

    private void Report(GeoServiceKind kind, bool isEnabled, string? url, GeoServiceDisabledReason disabledReason)
    {
        if (isEnabled)
        {
            var host = GeoServicesPolicyEvaluator.TryParseHttpUrl(url)?.Authority ?? string.Empty;
            if (_policy.NoOutboundTraffic)
            {
                _logger.LogWarning("Geo service {Kind}: enabled, host {Host}", kind, host);
            }
            else
            {
                _logger.LogInformation("Geo service {Kind}: enabled, host {Host}", kind, host);
            }

            return;
        }

        if (disabledReason == GeoServiceDisabledReason.InvalidConfiguration)
        {
            _logger.LogError(
                "Geo service {Kind}: disabled, the configured URL is not an absolute http(s) URL without credentials or the GeoServices section cannot be read",
                kind);
        }
        else
        {
            _logger.LogWarning("Geo service {Kind}: disabled ({Reason})", kind, disabledReason);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
