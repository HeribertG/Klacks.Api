// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks an endpoint that the browser reaches with the refresh token cookie (refresh, logout, password change), so
/// RefreshCookieRequestGuardFilter checks the X-Requested-With header and the Origin before the action runs.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RefreshCookieRequestGuardAttribute : ServiceFilterAttribute
{
    public RefreshCookieRequestGuardAttribute()
        : base(typeof(RefreshCookieRequestGuardFilter))
    {
    }
}
