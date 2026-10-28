// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter behind BlockedOnPlayground: on the Playground instance every visitor is the shared demo admin,
/// so admin-only LLM, provider and speech endpoints would let anybody spend the operator's money or change the
/// shared setup. The filter answers HTTP 403 with errorCode PLAYGROUND_FEATURE_BLOCKED before the action runs.
/// With the Playground mode off it does nothing at all.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Domain.Interfaces.Playground;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class BlockedOnPlaygroundFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;

    public BlockedOnPlaygroundFilter(IPlaygroundMode mode)
    {
        _mode = mode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_mode.IsActive)
        {
            await next();
            return;
        }

        context.Result = PlaygroundProblemResultFactory.FeatureBlocked();
    }
}
