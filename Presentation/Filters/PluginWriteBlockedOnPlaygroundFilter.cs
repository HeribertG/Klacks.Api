// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Global action filter: on the Playground instance every visitor is the shared demo admin, and plugin controllers are
/// registered as application parts whether the plugin is enabled or not, so their write endpoints (provider URLs,
/// connection tests, outbound sends) would be open to anybody. The filter answers every request with a method other
/// than GET, HEAD or OPTIONS to a controller that does not live in the Api assembly with the same HTTP 403
/// (errorCode PLAYGROUND_FEATURE_BLOCKED) as BlockedOnPlaygroundFilter. With the Playground mode off it does nothing.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Domain.Interfaces.Playground;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class PluginWriteBlockedOnPlaygroundFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;

    public PluginWriteBlockedOnPlaygroundFilter(IPlaygroundMode mode)
    {
        _mode = mode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_mode.IsActive && IsWriteMethod(context) && IsPluginController(context))
        {
            context.Result = PlaygroundProblemResultFactory.FeatureBlocked();
            return;
        }

        await next();
    }

    private static bool IsWriteMethod(ActionExecutingContext context)
    {
        var method = context.HttpContext.Request.Method;
        return !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method);
    }

    private static bool IsPluginController(ActionExecutingContext context) =>
        context.ActionDescriptor is ControllerActionDescriptor descriptor
        && descriptor.ControllerTypeInfo.Assembly != typeof(PluginWriteBlockedOnPlaygroundFilter).Assembly;
}
