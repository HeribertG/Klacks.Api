// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Swaps the default IHttpClientFactory logging of every HttpClient (named, typed and plugin clients,
/// whenever they are registered) for RedactingHttpClientLogger, so no request URI reaches a log with a
/// credential in its query or path.
/// </summary>
/// <param name="services">Service collection the client defaults are configured on</param>
using Klacks.Api.Infrastructure.Http;

namespace Klacks.Api.Infrastructure.Extensions;

public static class HttpClientLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddRedactedHttpClientLogging(this IServiceCollection services)
    {
        services.AddSingleton<RedactingHttpClientLogger>();
        services.ConfigureHttpClientDefaults(builder => builder
            .RemoveAllLoggers()
            .AddLogger<RedactingHttpClientLogger>());
        return services;
    }
}
