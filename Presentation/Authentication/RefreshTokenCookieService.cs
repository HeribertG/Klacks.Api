// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads, writes and deletes the HttpOnly cookie that carries the session refresh token. Writing and deleting use the
/// same attributes (Path, Secure, SameSite, HttpOnly), because a browser only removes a cookie whose attributes match.
/// The cookie is Secure when the browser reached the installation over HTTPS: either the request itself is HTTPS (after
/// ForwardedHeaders for a trusted proxy) or the first X-Forwarded-Proto value is https. The header is honoured from any
/// peer because it can only switch Secure on, which affects nothing but the caller's own cookie. Over plain HTTP (native
/// office PC on loopback, an on-premise install without TLS on the LAN) the cookie is not Secure, since a browser
/// would drop a Secure cookie there and end the session at the next refresh.
/// </summary>
/// <param name="options">SameSite mode from Auth:RefreshCookie</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Presentation.Authentication;

public sealed class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private const char ForwardedValueSeparator = ',';

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

    private static bool RequiresSecure(HttpRequest request)
    {
        return request.IsHttps || IsForwardedOverHttps(request);
    }

    private CookieOptions BuildOptions(HttpRequest request) => new()
    {
        HttpOnly = true,
        Path = RefreshTokenCookieConstants.CookiePath,
        Secure = RequiresSecure(request),
        SameSite = _options.SameSite,
        IsEssential = true,
    };

    private static bool IsForwardedOverHttps(HttpRequest request)
    {
        var forwardedProto = request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName].ToString();
        if (string.IsNullOrEmpty(forwardedProto))
        {
            return false;
        }

        var firstHop = forwardedProto.Split(ForwardedValueSeparator)[0].Trim();
        return string.Equals(firstHop, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
