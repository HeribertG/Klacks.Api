// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name and scope of the HttpOnly cookie that carries the access JWT for browser sessions. Path "/" because every API
/// route, the SignalR hubs and the STT WebSocket accept it.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class AccessTokenCookieConstants
{
    public const string CookieName = "klacks_access";

    public const string CookiePath = "/";
}
