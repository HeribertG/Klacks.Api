// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared building block for the permission gates. Every named policy that asserts a permission (RequireAssistant,
/// RequireAutomationManagement, RequireAccessTokenManagement) and the MCP endpoint's inline policy (which
/// additionally pins its own authentication schemes) go through RequirePermission instead of repeating the assertion.
///
/// Authentication is required before the permission is asserted. A policy named on an [Authorize]
/// attribute REPLACES the default policy rather than adding to it — AuthorizationPolicy.CombineAsync
/// folds the default policy in only for an attribute that carries neither a policy nor roles — so a
/// policy built from an assertion alone is evaluated for a failed or absent authentication too, with an
/// empty ClaimsPrincipal. That empty principal expands to the planner floor, CanUseAssistant included,
/// which would open ChatController, SkillsController, AgentPlansController and the MCP endpoint to
/// anonymous callers. RequireAuthenticatedUser contributes the DenyAnonymousAuthorizationRequirement
/// that keeps the assertion behind a real identity.
/// </summary>

using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Extensions;

public static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Adds a requirement that the caller has assistant access.
    /// </summary>
    /// <param name="policy">The policy under construction</param>
    public static AuthorizationPolicyBuilder RequireAssistantAccess(this AuthorizationPolicyBuilder policy)
    {
        return policy.RequirePermission(Permissions.CanUseAssistant);
    }

    /// <summary>
    /// Adds a requirement that the authenticated caller holds the given permission.
    /// </summary>
    /// <param name="policy">The policy under construction</param>
    /// <param name="permission">A constant from <see cref="Permissions"/> the authenticated caller must hold</param>
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder policy, string permission)
    {
        return policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                Permissions.HasPermission(context.User.GetUserRights(), permission));
    }
}
