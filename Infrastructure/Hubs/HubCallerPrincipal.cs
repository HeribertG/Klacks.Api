// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Claims;
using Klacks.Api.Domain.Common;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Makes the connection's own user the caller of a hub invocation (ExecutionPrincipal), so visibility guards judge that
/// user and never an empty request context: IHttpContextAccessor may be empty inside a hub invocation (long polling), and
/// a guard without a user would then treat the call as an unrestricted background caller.
/// </summary>
public static class HubCallerPrincipal
{
    /// <summary>
    /// Starts the caller scope; null when the connection carries no user id, which the hub must refuse.
    /// </summary>
    /// <param name="context">The hub caller context of the invocation</param>
    public static IDisposable? Begin(HubCallerContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var callerId) || callerId == Guid.Empty)
        {
            return null;
        }

        return ExecutionPrincipal.Begin(callerId);
    }
}
