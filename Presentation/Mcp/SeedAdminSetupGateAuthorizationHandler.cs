// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Succeeds SeedAdminSetupGateRequirement for every caller except the seeded admin account while the
/// own-admin setup gate is active; that caller stays unsucceeded, so the policy answers 403.
/// The gate service is only consulted when one of the caller's identities is the seeded admin.
/// </summary>
/// <param name="gateService">Reports whether the own-admin setup gate is still active (scoped)</param>

using System.Security.Claims;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Mcp;

public sealed class SeedAdminSetupGateAuthorizationHandler : AuthorizationHandler<SeedAdminSetupGateRequirement>
{
    private readonly IAdminSetupGateService _gateService;

    public SeedAdminSetupGateAuthorizationHandler(IAdminSetupGateService gateService)
    {
        _gateService = gateService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SeedAdminSetupGateRequirement requirement)
    {
        var isSeedAdmin = context.User.HasClaim(ClaimTypes.NameIdentifier, SystemAccounts.SeedAdminUserId);
        if (isSeedAdmin && await _gateService.IsGateActiveAsync())
        {
            return;
        }

        context.Succeed(requirement);
    }
}
