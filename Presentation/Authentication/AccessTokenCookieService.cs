// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads, writes and deletes the HttpOnly cookie that carries the access JWT of a browser session. Written next to the
/// refresh token cookie by login, refresh, OAuth2 callback and password change; JwtBearer falls back to it only when a
/// request carries neither an Authorization header nor a hub/STT query token. The attributes (HttpOnly, Secure iff
/// HTTPS, SameSite, IsEssential) come from AuthCookieAttributes; Path is "/" and Max-Age the access token lifetime.
/// </summary>
/// <param name="options">SameSite mode from Auth:RefreshCookie, shared by both session cookies</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Presentation.Authentication;

public sealed class AccessTokenCookieService : IAccessTokenCookieService
{
    private readonly RefreshTokenCookieOptions _options;

    public AccessTokenCookieService(IOptions<RefreshTokenCookieOptions> options)
    {
        _options = options.Value;
    }

    public string? Read(HttpRequest request)
    {
        var value = request.Cookies[AccessTokenCookieConstants.CookieName];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public void Write(HttpContext context, string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        var cookieOptions = BuildOptions(context.Request);
        cookieOptions.MaxAge = AccessTokenLifetime.Duration;
        context.Response.Cookies.Append(AccessTokenCookieConstants.CookieName, accessToken, cookieOptions);
    }

    public void Delete(HttpContext context)
    {
        context.Response.Cookies.Delete(AccessTokenCookieConstants.CookieName, BuildOptions(context.Request));
    }

    private CookieOptions BuildOptions(HttpRequest request) =>
        AuthCookieAttributes.Create(request, AccessTokenCookieConstants.CookiePath, _options.SameSite);
}
