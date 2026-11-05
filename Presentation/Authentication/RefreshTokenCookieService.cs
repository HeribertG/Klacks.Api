// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads, writes and deletes the HttpOnly cookie that carries the session refresh token. The attributes (HttpOnly,
/// Secure iff HTTPS, SameSite, IsEssential) come from AuthCookieAttributes, shared with the access token cookie; this
/// cookie is scoped to the Accounts path and lives as long as a refresh token.
/// </summary>
/// <param name="options">SameSite mode from Auth:RefreshCookie</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Presentation.Authentication;

public sealed class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private readonly RefreshTokenCookieOptions _options;

    public RefreshTokenCookieService(IOptions<RefreshTokenCookieOptions> options)
    {
        _options = options.Value;
    }

    public string? Read(HttpRequest request)
    {
        var value = request.Cookies[RefreshTokenCookieConstants.CookieName];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public void Write(HttpContext context, string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var cookieOptions = BuildOptions(context.Request);
        cookieOptions.MaxAge = RefreshTokenLifetime.Duration;
        context.Response.Cookies.Append(RefreshTokenCookieConstants.CookieName, refreshToken, cookieOptions);
    }

    public void Delete(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshTokenCookieConstants.CookieName, BuildOptions(context.Request));
    }

    private CookieOptions BuildOptions(HttpRequest request) =>
        AuthCookieAttributes.Create(request, RefreshTokenCookieConstants.CookiePath, _options.SameSite);
}
