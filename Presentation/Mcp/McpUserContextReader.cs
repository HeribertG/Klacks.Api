// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts the Klacks user identity (user id, tenant id, name) from the JWT claims principal of
/// an authenticated MCP request and resolves its effective permissions, expanding the caller's
/// role(s) the same way the chat pipeline does but capped at the Authorised (Supervisor) level
/// regardless of the caller's actual role.
/// </summary>
/// <param name="user">Claims principal of the current HTTP request; null yields an anonymous context</param>

using System.Security.Claims;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Presentation.Mcp;

public static class McpUserContextReader
{
    private const string TenantIdClaimType = "tenant_id";
    private const string UnknownUserName = "Unknown";

    // MCP is an external-agent channel: even an Admin's personal access token must never grant
    // more than Supervisor (Authorised) rights over MCP, so Admin-only permissions (deletes,
    // settings, user/identity-provider management) can never reach an external LLM through it.
    private static readonly HashSet<string> McpPermissionCeiling =
        new(Permissions.GetPermissionsForRole(Roles.Authorised), StringComparer.Ordinal) { Roles.Authorised };

    public static McpUserContext Read(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return new McpUserContext(Guid.Empty, Guid.Empty, UnknownUserName, new List<string>());
        }

        var userId = ParseGuidClaim(user, ClaimTypes.NameIdentifier);
        var tenantId = ParseGuidClaim(user, TenantIdClaimType);
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? UnknownUserName;
        var permissions = ResolveCappedPermissions(user);

        return new McpUserContext(userId, tenantId, userName, permissions);
    }

    private static List<string> ResolveCappedPermissions(ClaimsPrincipal user)
    {
        var expanded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in user.FindAll(ClaimTypes.Role).Select(claim => claim.Value))
        {
            expanded.Add(role);
            foreach (var permission in Permissions.GetPermissionsForRole(role))
            {
                expanded.Add(permission);
            }
        }

        expanded.IntersectWith(McpPermissionCeiling);

        return expanded.ToList();
    }

    private static Guid ParseGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var claim = user.FindFirst(claimType);
        if (claim != null && Guid.TryParse(claim.Value, out var value))
        {
            return value;
        }

        return Guid.Empty;
    }
}
