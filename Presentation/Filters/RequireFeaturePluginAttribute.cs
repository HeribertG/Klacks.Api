// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Action filter attribute that blocks requests when a feature plugin is not enabled.
/// Returns 404 NotFound if the plugin is disabled or not installed.
/// </summary>
/// <param name="pluginName">The plugin name to check (must match manifest name)</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeaturePluginAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _pluginName;

    public RequireFeaturePluginAttribute(string pluginName)
    {
        _pluginName = pluginName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var pluginService = context.HttpContext.RequestServices.GetRequiredService<IFeaturePluginService>();

        if (!pluginService.IsEnabled(_pluginName))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
