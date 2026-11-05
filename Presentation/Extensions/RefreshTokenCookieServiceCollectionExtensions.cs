// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the refresh token cookie: its options from Auth:RefreshCookie, the cookie service and the CSRF guard filter
/// of the endpoints that read the cookie.
/// </summary>
/// <param name="services">Service collection of the host</param>
/// <param name="configuration">Configuration holding the Auth:RefreshCookie section</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Presentation.Authentication;
using Klacks.Api.Presentation.Filters;

namespace Klacks.Api.Presentation.Extensions;

public static class RefreshTokenCookieServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksRefreshTokenCookie(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RefreshTokenCookieOptions>(configuration.GetSection(RefreshTokenCookieOptions.SectionName));
        services.AddSingleton<IRefreshTokenCookieService, RefreshTokenCookieService>();
        services.AddTransient<RefreshCookieRequestGuardFilter>();
        return services;
    }
}
