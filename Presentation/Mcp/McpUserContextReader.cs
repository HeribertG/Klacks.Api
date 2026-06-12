// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts the Klacks user identity (user id, tenant id, name, role permissions) from the
/// JWT claims principal of an authenticated MCP request.
/// </summary>
/// <param name="user">Claims principal of the current HTTP request; null yields an anonymous context</param>

using System.Security.Claims;

namespace Klacks.Api.Presentation.Mcp;

public static class McpUserContextReader
{
    private const string TenantIdClaimType = "tenant_id";
    private const string UnknownUserName = "Unknown";

    public static McpUserContext Read(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return new McpUserContext(Guid.Empty, Guid.Empty, UnknownUserName, new List<string>());
        }

        var userId = ParseGuidClaim(user, ClaimTypes.NameIdentifier);
        var tenantId = ParseGuidClaim(user, TenantIdClaimType);
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? UnknownUserName;
        var permissions = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();

        return new McpUserContext(userId, tenantId, userName, permissions);
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
