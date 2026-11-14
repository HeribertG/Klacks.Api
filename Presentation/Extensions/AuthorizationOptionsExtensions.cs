// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single registration point for every named authorization policy of the application. Program.cs calls
/// nothing else inside AddAuthorization, so the architecture guard
/// (Klacks.UnitTest/Architecture/AuthorizationPolicyAnonymousGuardTests) can build the very same
/// AuthorizationOptions the host builds and check each policy named in AuthorizationPolicies — a policy
/// declared here but never registered, or registered without a DenyAnonymousAuthorizationRequirement,
/// fails there instead of silently letting an anonymous caller through.
/// </summary>

using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Extensions;

public static class AuthorizationOptionsExtensions
{
    /// <summary>
    /// Adds the Klacks authorization policies to the options.
    /// </summary>
    /// <param name="options">The options instance AddAuthorization hands to its configuration callback</param>
    public static AuthorizationOptions AddKlacksPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.RequireAssistant, policy => policy.RequireAssistantAccess());
        options.AddPolicy(AuthorizationPolicies.RequireAutomationManagement,
            policy => policy.RequirePermission(Permissions.CanManageAutomation));
        options.AddPolicy(AuthorizationPolicies.RequireAccessTokenManagement,
            policy => policy.RequirePermission(Permissions.CanManageAccessTokens));
        options.AddPolicy(AuthorizationPolicies.RequireScheduleEditing,
            policy => policy.RequirePermission(Permissions.CanEditSchedule));

        return options;
    }
}
