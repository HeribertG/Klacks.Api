// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// CSRF guard of the endpoints that read the refresh token cookie. A request is answered with 403 when
/// - its Origin header is present and is neither an origin of the CORS policy nor on the host the request was sent to.
///   The same-host check compares the hostname only, ignoring scheme and port on purpose: nginx forwards "Host $host"
///   without the port, so an on-premise install on a non-standard port would otherwise never match and log users out;
///   on-premise servers are also reached under names the CORS list does not know. It is defense in depth on top of the
///   second rule, or
/// - it carries neither "X-Requested-With: XMLHttpRequest" nor the content type application/json. Both force a CORS
///   preflight on a cross-origin request, which the explicit origin list rejects, while a form post, text/plain, a
///   beacon or a request without content type does not. The JSON alternative keeps browser tabs that still run the
///   previous Ui (JSON body, no header) working during a deploy.
/// </summary>
/// <param name="corsPolicyProvider">Resolves the application's CORS policy, the single source of the allowed origins</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using System.Net.Mime;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Presentation.Filters;

public sealed class RefreshCookieRequestGuardFilter : IAsyncActionFilter
{
    private static readonly char[] Ipv6Brackets = ['[', ']'];

    private readonly ICorsPolicyProvider _corsPolicyProvider;

    public RefreshCookieRequestGuardFilter(ICorsPolicyProvider corsPolicyProvider)
    {
        _corsPolicyProvider = corsPolicyProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!await IsOriginAcceptedAsync(context.HttpContext)
            || (!HasRequestedWithHeader(request) && !IsJsonRequest(request)))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }

    private async Task<bool> IsOriginAcceptedAsync(HttpContext httpContext)
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
            request.Headers[RefreshTokenCookieConstants.RequestedWithHeader].ToString(),
            RefreshTokenCookieConstants.RequestedWithValue,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonRequest(HttpRequest request)
    {
        return MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            && string.Equals(contentType.MediaType.Value, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase);
    }
}
