// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the Idempotency-Key handling of the creating endpoints: the instance-wide store as singleton and the
/// filter that [IdempotentRequest] resolves from DI.
/// </summary>
/// <param name="services">Service collection of the host</param>

using Klacks.Api.Presentation.Filters;

namespace Klacks.Api.Presentation.Extensions;

public static class IdempotentRequestServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksIdempotentRequests(this IServiceCollection services)
    {
        services.AddSingleton<IdempotentRequestStore>();
        services.AddTransient<IdempotentRequestFilter>();
        return services;
    }
}
