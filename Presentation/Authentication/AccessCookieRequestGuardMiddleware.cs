// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Global CSRF/CSWSH guard of requests authenticated by the klacks_access cookie. Runs after UseAuthorization, because
/// JwtBearer only runs inside the authorization middleware (the default authenticate scheme is the Identity cookie).
/// A cookie-authenticated request is answered with 403 when
/// - CookieCsrfRules does not accept its origin (any method; a WebSocket upgrade to a hub or the STT stream is judged by
///   Sec-Fetch-Site first, see CookieCsrfRules), or
/// - its method is not GET, HEAD, OPTIONS or TRACE and it carries no CSRF proof (X-Requested-With or JSON).
/// Requests authenticated by an Authorization header or a hub/STT query token are not touched.
/// </summary>
/// <param name="next">Next middleware of the pipeline</param>
/// <param name="logger">Records rejected requests</param>

using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Presentation.Authentication;

public sealed class AccessCookieRequestGuardMiddleware
{
    private const string ForeignOriginReason = "foreign origin";
    private const string MissingProofReason = "no CSRF proof";

    private readonly RequestDelegate _next;
    private readonly ILogger<AccessCookieRequestGuardMiddleware> _logger;

    public AccessCookieRequestGuardMiddleware(RequestDelegate next, ILogger<AccessCookieRequestGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CookieCsrfRules rules)
    {
        if (AccessTokenCookieAuthentication.IsCookieAuthenticated(context))
        {
            if (!await rules.IsOriginAcceptedAsync(context))
            {
                Reject(context, ForeignOriginReason);
                return;
            }

            if (!IsSafeMethod(context.Request.Method) && !CookieCsrfRules.CarriesCsrfProof(context.Request))
            {
                Reject(context, MissingProofReason);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method)
            || HttpMethods.IsTrace(method);
    }

    private void Reject(HttpContext context, string reason)
    {
        _logger.LogWarning("Cookie-authenticated request rejected ({Reason}): {Method} {Path}",
            reason, context.Request.Method.ForLog(), context.Request.Path.ToString().ForLog());
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
