// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks an action whose free-text input is capped on the Playground, so PlaygroundPromptLimitFilter rejects an
/// over-long message before any provider call. The filter is resolved from DI; without the Playground mode it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PlaygroundPromptLimitAttribute : ServiceFilterAttribute
{
    public PlaygroundPromptLimitAttribute()
        : base(typeof(PlaygroundPromptLimitFilter))
    {
    }
}
