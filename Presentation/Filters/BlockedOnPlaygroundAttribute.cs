// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a controller or action as unavailable on the public Playground, so BlockedOnPlaygroundFilter answers it
/// with HTTP 403 there. The filter is resolved from DI; without the Playground mode it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BlockedOnPlaygroundAttribute : ServiceFilterAttribute
{
    public BlockedOnPlaygroundAttribute()
        : base(typeof(BlockedOnPlaygroundFilter))
    {
    }
}
