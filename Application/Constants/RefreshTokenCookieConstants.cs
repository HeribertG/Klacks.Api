// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name and scope of the HttpOnly cookie that carries the session refresh token. The CSRF proof headers the
/// cookie-reading endpoints demand are in CsrfProofConstants.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class RefreshTokenCookieConstants
{
    public const string CookieName = "klacks_refresh";

    public const string CookiePath = "/api/backend/Accounts";
}
