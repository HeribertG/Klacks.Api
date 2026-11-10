// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a request comes straight from the machine Klacks runs on: the remote address is loopback and the
/// request carries none of the headers a reverse proxy adds (X-Forwarded-For, X-Original-For left by the forwarded
/// headers middleware, Forwarded, X-Real-IP). A native office-PC installation answers its own browser this way; every
/// request through nginx or from another machine counts as remote, so the decision fails closed.
/// </summary>

using Klacks.Api.Application.Helpers;
using Microsoft.AspNetCore.HttpOverrides;

namespace Klacks.Api.Presentation.Authentication;

public static class DirectLoopbackRequest
{
    private const string ForwardedHeaderName = "Forwarded";
    private const string RealIpHeaderName = "X-Real-IP";

    private static readonly string[] ProxyHeaderNames =
    [
        ForwardedHeadersDefaults.XForwardedForHeaderName,
        ForwardedHeadersDefaults.XOriginalForHeaderName,
        ForwardedHeaderName,
        RealIpHeaderName,
    ];

    public static bool IsDirectLoopback(HttpContext context) =>
        LoopbackAddress.IsLoopback(context.Connection.RemoteIpAddress)
        && !ProxyHeaderNames.Any(context.Request.Headers.ContainsKey);
}
