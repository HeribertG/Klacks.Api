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
    private static readonly GeoServiceKind[] ReportedKinds =
    [
        GeoServiceKind.Geocoding,
        GeoServiceKind.Routing,
        GeoServiceKind.OpenRouteService,
        GeoServiceKind.MapTiles
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

        foreach (var kind in ReportedKinds)
        {
            var endpoint = _policy.Get(kind);
            if (endpoint.IsEnabled)
            {
                var host = GeoServicesPolicyEvaluator.TryParseHttpUrl(endpoint.BaseUrl)?.Authority ?? string.Empty;
                if (_policy.NoOutboundTraffic)
                {
                    _logger.LogWarning("Geo service {Kind}: enabled, host {Host}", kind, host);
                }
                else
                {
                    _logger.LogInformation("Geo service {Kind}: enabled, host {Host}", kind, host);
                }

                continue;
            }

            if (endpoint.DisabledReason == GeoServiceDisabledReason.InvalidConfiguration)
            {
                _logger.LogError(
                    "Geo service {Kind}: disabled, the configured URL is not an absolute http(s) URL or the GeoServices section cannot be read",
                    kind);
            }
            else
            {
                _logger.LogWarning("Geo service {Kind}: disabled ({Reason})", kind, endpoint.DisabledReason);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
