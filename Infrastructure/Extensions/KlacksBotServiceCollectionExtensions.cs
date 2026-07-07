// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the Klacks bot token's own authentication scheme. Kept separate from the main JWT
/// AddAuthentication() call so the KlacksBotToken scheme never becomes a default candidate
/// for JWT-protected endpoints (the same scheme-pinning footgun documented for SignalR hubs,
/// the MCP endpoint and the ERP import token: AddIdentity overrides the default scheme).
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Infrastructure.Authentication;
using Klacks.Api.Infrastructure.Repositories.Bots;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class KlacksBotServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksBotAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, KlacksBotTokenAuthenticationHandler>(KlacksBotTokenConstants.SchemeName, configureOptions: null);

        services.AddScoped<IKlacksBotTokenRepository, KlacksBotTokenRepository>();

        return services;
    }
}
