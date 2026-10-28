// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks an action as spending LLM tokens, so PlaygroundTokenLimitFilter guards it on the Playground instance.
/// The filter is resolved from DI; without the Playground mode it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PlaygroundTokenLimitAttribute : ServiceFilterAttribute
{
    public PlaygroundTokenLimitAttribute()
        : base(typeof(PlaygroundTokenLimitFilter))
    {
    }
}
