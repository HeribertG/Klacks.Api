// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the geo service policy and the HTTP clients for the OpenStreetMap-based services (Nominatim
/// geocoding, OSRM routing, OpenRouteService). Every client carries the Klacks User-Agent, without which the
/// public servers answer 403, and the central outbound lock that only lets requests to enabled endpoints pass.
/// </summary>
/// <param name="services">Service collection the clients and the policy are added to</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Services.GeoServices;
using Klacks.Api.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ExternalHttpClientServiceCollectionExtensions
{
    private static readonly GeoServiceKind[] NominatimClientKinds = [GeoServiceKind.Geocoding];
    private static readonly GeoServiceKind[] RoutingClientKinds = [GeoServiceKind.Routing, GeoServiceKind.OpenRouteService];
    private static readonly GeoServiceKind[] OpenRouteServiceClientKinds = [GeoServiceKind.OpenRouteService];

    public static IServiceCollection AddExternalHttpClients(this IServiceCollection services)
    {
        services.AddSingleton<IGeoServicesPolicy, GeoServicesPolicy>();

        services.AddHttpClient(ExternalHttpClientConstants.NominatimClientName, ApplyUserAgent)
            .AddHttpMessageHandler(sp => CreateGuard(sp, NominatimClientKinds));
        services.AddHttpClient(ExternalHttpClientConstants.RoutingClientName, ApplyUserAgent)
            .AddHttpMessageHandler(sp => CreateGuard(sp, RoutingClientKinds));
        return services;
    }

    public static IHttpClientBuilder AddOpenRouteServiceGuard(this IHttpClientBuilder builder)
    {
        return builder
            .ConfigureHttpClient(ApplyUserAgent)
            .AddHttpMessageHandler(sp => CreateGuard(sp, OpenRouteServiceClientKinds));
    }

    private static GeoServiceOutboundGuardHandler CreateGuard(IServiceProvider serviceProvider, GeoServiceKind[] kinds)
    {
        return new GeoServiceOutboundGuardHandler(
            serviceProvider.GetRequiredService<IGeoServicesPolicy>(),
            kinds,
            serviceProvider.GetRequiredService<ILogger<GeoServiceOutboundGuardHandler>>());
    }

    private static void ApplyUserAgent(HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ExternalHttpClientConstants.UserAgent);
    }
}
