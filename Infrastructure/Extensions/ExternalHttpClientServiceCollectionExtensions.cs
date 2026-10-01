// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the named HTTP clients for the public OpenStreetMap-based services (Nominatim geocoding and
/// OSRM/OpenRouteService routing). Both carry the Klacks User-Agent, without which the public servers answer 403.
/// </summary>
/// <param name="services">Service collection the named clients are added to</param>
using Klacks.Api.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ExternalHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddExternalHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(ExternalHttpClientConstants.NominatimClientName, ApplyUserAgent);
        services.AddHttpClient(ExternalHttpClientConstants.RoutingClientName, ApplyUserAgent);
        return services;
    }

    private static void ApplyUserAgent(HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ExternalHttpClientConstants.UserAgent);
    }
}
