// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the user an answer in the replacement request book is recorded under from the request's identity
/// claim. Null without an HTTP context or with a non-Guid claim (background callers); the fact is then stored
/// without an author rather than refused.
/// </summary>
/// <param name="httpContextAccessor">Current request</param>

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public static class ReplacementRequestActor
{
    public static Guid? CurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var claimValue = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
