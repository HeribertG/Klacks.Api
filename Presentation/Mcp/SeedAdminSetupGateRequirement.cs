// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Authorization requirement on the /mcp endpoint policy that denies the seeded admin account while its
/// own-admin setup gate is active. The MVC AdminSetupGateFilter does not run for /mcp (MapMcp sits outside
/// the MVC filter pipeline), so this requirement closes that gap for every scheme the policy accepts:
/// login JWT, personal access token and OAuth-issued token.
/// </summary>

using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Mcp;

public sealed class SeedAdminSetupGateRequirement : IAuthorizationRequirement
{
    public static readonly SeedAdminSetupGateRequirement Instance = new();

    private SeedAdminSetupGateRequirement()
    {
    }
}
