// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The CSRF/CSWSH rules of every request a browser authenticates with a session cookie, shared by the refresh-cookie
/// endpoint filter and the global access-cookie guard:
/// - Origin of an ordinary request: accepted when absent, when it names the host the request was sent to, or when the
///   CORS policy allows it. The same-host check compares the hostname only, ignoring scheme and port on purpose: nginx
///   forwards "Host $host" without the port, so an on-premise install on a non-standard port would otherwise never match
///   and log users out; on-premise servers are also reached under names the CORS list does not know.
/// - Origin of a WebSocket upgrade (hubs, STT stream): a WebSocket is not subject to CORS, so the hostname-only rule would
///   let a page on another port of the same host open a socket with the user's cookie. Accepted when Sec-Fetch-Site is
///   same-origin (the browser compares scheme, host and port itself, so no proxy configuration is needed), else when the
///   CORS policy allows the Origin (development Ui on another port); only a browser that sends no Sec-Fetch-Site at all
///   falls back to the ordinary Origin rule. Everything else is rejected.
/// - Proof: "X-Requested-With: XMLHttpRequest" or the content type application/json. Both force a CORS preflight on a
///   cross-origin request, which the explicit origin list rejects, while a form post, text/plain, a beacon or a request
///   without content type does not.
/// </summary>
/// <param name="corsPolicyProvider">Resolves the application's CORS policy, the single source of the allowed origins</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using System.Net.Mime;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Presentation.Authentication;

public sealed class CookieCsrfRules
{
    private static readonly char[] Ipv6Brackets = ['[', ']'];

    private readonly ICorsPolicyProvider _corsPolicyProvider;

    public CookieCsrfRules(ICorsPolicyProvider corsPolicyProvider)
    {
        _corsPolicyProvider = corsPolicyProvider;
    }

    public Task<bool> IsOriginAcceptedAsync(HttpContext httpContext)
    {
        return httpContext.WebSockets.IsWebSocketRequest
            ? IsWebSocketOriginAcceptedAsync(httpContext)
            : IsRequestOriginAcceptedAsync(httpContext);
    }

    public static bool CarriesCsrfProof(HttpRequest request)
    {
        return HasRequestedWithHeader(request) || IsJsonRequest(request);
    }

    private async Task<bool> IsWebSocketOriginAcceptedAsync(HttpContext httpContext)
    {
        var fetchSite = httpContext.Request.Headers[CsrfProofConstants.FetchSiteHeader].ToString();
        if (string.IsNullOrEmpty(fetchSite))
        {
            return await IsRequestOriginAcceptedAsync(httpContext);
        }

        if (string.Equals(fetchSite, CsrfProofConstants.FetchSiteSameOrigin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var origin = httpContext.Request.Headers[HeaderNames.Origin].ToString();
        return !string.IsNullOrEmpty(origin) && await IsCorsOriginAsync(httpContext, origin);
    }

    private async Task<bool> IsRequestOriginAcceptedAsync(HttpContext httpContext)
    {
        var origin = httpContext.Request.Headers[HeaderNames.Origin].ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        if (IsSameHost(origin, httpContext.Request))
        {
            return true;
        }

        return await IsCorsOriginAsync(httpContext, origin);
    }

    private async Task<bool> IsCorsOriginAsync(HttpContext httpContext, string origin)
    {
        var policy = await _corsPolicyProvider.GetPolicyAsync(httpContext, CorsOriginsOptions.PolicyName);
        return policy != null && policy.IsOriginAllowed(origin);
    }

    private static bool IsSameHost(string origin, HttpRequest request)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            && !string.IsNullOrEmpty(request.Host.Host)
            && string.Equals(originUri.Host.Trim(Ipv6Brackets), request.Host.Host.Trim(Ipv6Brackets), StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasRequestedWithHeader(HttpRequest request)
    {
        return string.Equals(
            request.Headers[CsrfProofConstants.RequestedWithHeader].ToString(),
            CsrfProofConstants.RequestedWithValue,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonRequest(HttpRequest request)
    {
        return MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            && string.Equals(contentType.MediaType.Value, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase);
    }
}
