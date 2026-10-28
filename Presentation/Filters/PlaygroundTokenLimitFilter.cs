// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter for the assistant endpoints that spend LLM tokens on the Playground. Before the action runs
/// (and so before a stream starts) it answers HTTP 429 with errorCode PLAYGROUND_TOKEN_LIMIT when a budget bucket
/// of the visitor is already used up; after a non-streaming action it turns a call the budget refused during the
/// action into the same answer. With the Playground mode off it does nothing at all.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>
/// <param name="budget">The daily token budget the pre-check reads</param>

using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Services.Playground;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class PlaygroundTokenLimitFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;
    private readonly IPlaygroundTokenBudget _budget;

    public PlaygroundTokenLimitFilter(IPlaygroundMode mode, IPlaygroundTokenBudget budget)
    {
        _mode = mode;
        _budget = budget;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_mode.IsActive)
        {
            await next();
            return;
        }

        var visitor = PlaygroundVisitorContext.Current;
        if (_budget.IsExhausted(visitor))
        {
            context.Result = PlaygroundProblemResultFactory.TokenLimitReached();
            return;
        }

        var executed = await next();
        if (visitor?.TokenLimitHit == true && executed.Exception is null && !context.HttpContext.Response.HasStarted)
        {
            executed.Result = PlaygroundProblemResultFactory.TokenLimitReached();
        }
    }
}
