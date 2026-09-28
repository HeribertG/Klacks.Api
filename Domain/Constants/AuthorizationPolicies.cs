// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires <see cref="Permissions.CanUseAssistant"/>. Every role holds it today, so the policy
    /// changes nothing for existing callers — it exists so that taking the assistant away from a role
    /// is a one-line change in the permission matrix instead of a new gate on every entry point.
    /// </summary>
    public const string RequireAssistant = "RequireAssistant";
}
