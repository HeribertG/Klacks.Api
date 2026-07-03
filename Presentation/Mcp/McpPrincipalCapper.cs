// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Downgrades an authenticated Admin principal to Authorised (Supervisor) for the MCP endpoint,
/// regardless of which scheme authenticated it (login JWT or personal access token) — a personal
/// access token mirrors its owner's real roles 1:1, so without this cap an Admin-owned token would
/// reach Admin-only code paths (e.g. period closing) through MCP even though McpUserContextReader's
/// permission-list cap does not cover role-based checks like IHttpContextAccessor.User.IsInRole.
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

        var claims = principal.Claims
            .Where(claim => !(claim.Type == ClaimTypes.Role && claim.Value == Roles.Admin))
            .ToList();

        if (!principal.IsInRole(Roles.Authorised))
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Authorised));
        }

        var cappedIdentity = new ClaimsIdentity(claims, principal.Identity.AuthenticationType);

        return new ClaimsPrincipal(cappedIdentity);
    }
}
