// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request-scoped record, kept in HttpContext.Items, of whether JwtBearer took the access token from the klacks_access
/// cookie and whether that token validated. Only a validated cookie token makes the request cookie-authenticated, which
/// puts it under AccessCookieRequestGuardMiddleware; an invalid cookie grants nothing and is not guarded.
/// </summary>
namespace Klacks.Api.Presentation.Authentication;

public static class AccessTokenCookieAuthentication
{
    private static readonly object TokenFromCookieKey = new();
    private static readonly object AuthenticatedByCookieKey = new();

    public static void MarkTokenFromCookie(HttpContext context)
    {
        context.Items[TokenFromCookieKey] = true;
    }

    public static void ConfirmIfTokenFromCookie(HttpContext context)
    {
        if (context.Items.ContainsKey(TokenFromCookieKey))
        {
            context.Items[AuthenticatedByCookieKey] = true;
        }
    }

    public static bool IsCookieAuthenticated(HttpContext context)
    {
        return context.Items.ContainsKey(AuthenticatedByCookieKey);
    }
}
