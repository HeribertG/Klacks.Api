// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the attributes of the session cookies (access token and refresh token), the one place for HttpOnly, Secure,
/// SameSite and IsEssential. Writing and deleting a cookie use the same attributes, because a browser only removes a
/// cookie whose attributes match. The cookie is Secure when the browser reached the installation over HTTPS: either the
/// request itself is HTTPS (after ForwardedHeaders for a trusted proxy) or the first X-Forwarded-Proto value is https.
/// The header is honoured from any peer because it can only switch Secure on, which affects nothing but the caller's own
/// cookie. Over plain HTTP (native office PC on loopback, an on-premise install without TLS on the LAN) the cookie is not
/// Secure, since a browser would drop a Secure cookie there and end the session.
/// </summary>
/// <param name="request">The request the cookie is written or deleted for</param>
/// <param name="path">Path attribute of the cookie</param>
/// <param name="sameSite">SameSite mode from Auth:RefreshCookie</param>

using Microsoft.AspNetCore.HttpOverrides;

namespace Klacks.Api.Presentation.Authentication;

public static class AuthCookieAttributes
{
    private const char ForwardedValueSeparator = ',';

    public static CookieOptions Create(HttpRequest request, string path, SameSiteMode sameSite) => new()
    {
        HttpOnly = true,
        Path = path,
        Secure = RequiresSecure(request),
        SameSite = sameSite,
        IsEssential = true,
    };

    private static bool RequiresSecure(HttpRequest request)
    {
        return request.IsHttps || IsForwardedOverHttps(request);
    }

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
