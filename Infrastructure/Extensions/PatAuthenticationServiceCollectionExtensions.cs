// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Wires the personal access token's own authentication scheme (PatConstants.SchemeName). Kept separate from the
/// main JWT AddAuthentication() call so the scheme never becomes a default candidate; it is pinned only in the
/// /mcp endpoint policy (MapKlacksMcp), which makes personal access tokens MCP-only.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class PatAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksPatAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, PatAuthenticationHandler>(PatConstants.SchemeName, configureOptions: null);

        return services;
    }
}
