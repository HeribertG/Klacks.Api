// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a creating action whose repeats with the same Idempotency-Key header must not create a second entry, so
/// IdempotentRequestFilter answers them from the first execution. The filter is resolved from DI; without the header it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : ServiceFilterAttribute
{
    public IdempotentRequestAttribute()
        : base(typeof(IdempotentRequestFilter))
    {
    }
}
