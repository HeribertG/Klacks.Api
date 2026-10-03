// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Endpoint metadata marking an endpoint whose authenticated principal must be capped at Authorised
/// (Supervisor) before the endpoint runs. Set by MapKlacksMcp and read by UseMcpPermissionCap, so the cap
/// follows the endpoint itself rather than a path string.
/// </summary>

namespace Klacks.Api.Presentation.Mcp;

public sealed class McpPermissionCapMetadata
{
    public static readonly McpPermissionCapMetadata Instance = new();

    private McpPermissionCapMetadata()
    {
    }
}
