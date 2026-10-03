// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Downgrades an authenticated Admin principal to Authorised (Supervisor) for the MCP endpoint,
/// regardless of which scheme authenticated it (login JWT, personal access token or OAuth token) — a personal
/// access token mirrors its owner's real roles 1:1, so without this cap an Admin-owned token would
/// reach Admin-only code paths (e.g. period closing) through MCP even though McpUserContextReader's
/// permission-list cap does not cover role-based checks like IHttpContextAccessor.User.IsInRole.
/// Every identity is rebuilt on its own with its authentication type and name/role claim types intact, so
/// McpAccessModeResolver still recognises a personal access token identity after the cap.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Presentation.Mcp;

public static class McpPrincipalCapper
{
    public static ClaimsPrincipal CapToAuthorised(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || !principal.IsInRole(Roles.Admin))
        {
            return principal;
        }

        var alreadyAuthorised = principal.IsInRole(Roles.Authorised);
        var cappedIdentities = new List<ClaimsIdentity>();

        foreach (var identity in principal.Identities)
        {
            var claims = identity.Claims
                .Where(claim => !IsAdminRoleClaim(identity, claim))
                .ToList();

            if (!alreadyAuthorised && identity.IsAuthenticated)
            {
                claims.Add(new Claim(identity.RoleClaimType, Roles.Authorised));
                alreadyAuthorised = true;
            }

            cappedIdentities.Add(new ClaimsIdentity(
                claims, identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType));
        }

        return new ClaimsPrincipal(cappedIdentities);
    }

    private static bool IsAdminRoleClaim(ClaimsIdentity identity, Claim claim)
    {
        return claim.Value == Roles.Admin
               && (claim.Type == identity.RoleClaimType || claim.Type == ClaimTypes.Role);
    }
}
