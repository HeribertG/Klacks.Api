// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name and scope of the HttpOnly cookie that carries the session refresh token, and the request header the
/// cookie-reading endpoints demand as CSRF protection. The Ui mirrors the header name and value.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class RefreshTokenCookieConstants
{
    public const string CookieName = "klacks_refresh";

    public const string CookiePath = "/api/backend/Accounts";

    public const string RequestedWithHeader = "X-Requested-With";

    public const string RequestedWithValue = "XMLHttpRequest";
}
