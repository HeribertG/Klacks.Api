// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the session cookies: their options from Auth:RefreshCookie, the refresh and access token cookie services,
/// the shared CSRF rules, the CSRF guard filter of the endpoints that read the refresh cookie and the JwtBearer events
/// that fall back to the access token cookie.
/// </summary>
/// <param name="services">Service collection of the host</param>
/// <param name="configuration">Configuration holding the Auth:RefreshCookie section</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Presentation.Authentication;
using Klacks.Api.Presentation.Filters;

namespace Klacks.Api.Presentation.Extensions;

public static class SessionCookieServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksSessionCookies(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RefreshTokenCookieOptions>(configuration.GetSection(RefreshTokenCookieOptions.SectionName));
        services.AddSingleton<IRefreshTokenCookieService, RefreshTokenCookieService>();
        services.AddSingleton<IAccessTokenCookieService, AccessTokenCookieService>();
        services.AddTransient<CookieCsrfRules>();
        services.AddTransient<RefreshCookieRequestGuardFilter>();
        services.AddSingleton<KlacksJwtBearerEvents>();
        return services;
    }
}
