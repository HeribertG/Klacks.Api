// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// CSRF guard of the endpoints that read the refresh token cookie. A request is answered with 403 when its Origin is not
/// accepted or it carries no CSRF proof (CookieCsrfRules: X-Requested-With or JSON). Unlike the global access-cookie
/// guard it applies to every request of these endpoints, whatever authenticated it, because they read the refresh cookie.
/// The JSON alternative keeps browser tabs that still run the previous Ui (JSON body, no header) working during a deploy.
/// </summary>
/// <param name="rules">The shared Origin and proof rules of cookie-authenticated requests</param>

using Klacks.Api.Presentation.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public sealed class RefreshCookieRequestGuardFilter : IAsyncActionFilter
{
    private readonly CookieCsrfRules _rules;

    public RefreshCookieRequestGuardFilter(CookieCsrfRules rules)
    {
        _rules = rules;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _rules.IsOriginAcceptedAsync(context.HttpContext)
            || !CookieCsrfRules.CarriesCsrfProof(context.HttpContext.Request))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }
}
