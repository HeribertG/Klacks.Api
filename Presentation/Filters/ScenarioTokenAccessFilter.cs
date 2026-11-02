// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Global action filter that keeps every HTTP endpoint inside the caller's scenarios: before the action runs it
/// collects all scenario tokens the request carries in its bound arguments (ScenarioTokenArgumentScanner) and checks
/// each distinct token with IScenarioTokenAccessGuard. A token of a scenario the caller may not see - or that does not
/// exist - ends the request with KeyNotFoundException, which ErrorHandlingMiddleware answers with 404 (never 401).
/// Admins and an installation without groups are unrestricted and pass for scenario tokens. The token of a variant (key)
/// is refused for everyone, admins included, unless the action carries AllowVariantTokenAttribute and the variant's
/// group is visible; on such an action every other token is still checked as usual.
/// </summary>
/// <param name="scenarioTokenAccessGuard">Loads the scenario behind a token and applies the group visibility rule</param>

using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class ScenarioTokenAccessFilter : IAsyncActionFilter
{
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public ScenarioTokenAccessFilter(IScenarioTokenAccessGuard scenarioTokenAccessGuard)
    {
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tokens = ScenarioTokenArgumentScanner.Collect(context.ActionArguments);
        var cancellationToken = context.HttpContext.RequestAborted;

        var allowVariant = context.ActionDescriptor.EndpointMetadata?.OfType<AllowVariantTokenAttribute>().Any() == true;

        foreach (var token in tokens)
        {
            if (allowVariant && await _scenarioTokenAccessGuard.IsVariantTokenVisibleAsync(token, cancellationToken))
            {
                continue;
            }

            await _scenarioTokenAccessGuard.EnsureVisibleAsync(token, cancellationToken);
        }

        await next();
    }
}
