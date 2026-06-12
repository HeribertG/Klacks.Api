// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Public base URL under which the MCP endpoint and the built-in OAuth authorization server
/// are reachable from the internet, bound from the "Mcp" configuration section. Used for the
/// RFC 9728 protected-resource metadata and the RFC 8414 authorization-server metadata.
/// </summary>
namespace Klacks.Api.Application.Configuration;

public class McpPublicEndpointOptions
{
    public const string SectionName = "Mcp";

    public const string DefaultPublicBaseUrl = "https://localhost:5001";

    public string PublicBaseUrl { get; set; } = DefaultPublicBaseUrl;

    public string NormalizedPublicBaseUrl => PublicBaseUrl.TrimEnd('/');
}
