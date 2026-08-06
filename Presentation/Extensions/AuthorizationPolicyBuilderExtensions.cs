// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared building block for the assistant gate. The named RequireAssistant policy and the MCP
/// endpoint's inline policy (which additionally pins its own authentication schemes) must apply the
/// same check, so both call this instead of repeating the assertion.
/// </summary>

using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Extensions;

public static class AuthorizationPolicyBuilderExtensions
{
    /// <param name="policy">The policy under construction</param>
    public static AuthorizationPolicyBuilder RequireAssistantAccess(this AuthorizationPolicyBuilder policy)
    {
        return policy.RequireAssertion(context =>
            Permissions.HasPermission(context.User.GetUserRights(), Permissions.CanUseAssistant));
    }
}
