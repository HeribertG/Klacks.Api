// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the MCP (Model Context Protocol) endpoint: server identity, route and client instructions.
/// </summary>

namespace Klacks.Api.Presentation.Mcp;

public static class McpServerConstants
{
    public const string ServerName = "klacks-mcp";
    public const string ServerTitle = "Klacks MCP Server";
    public const string ServerVersion = "1.0.0";
    public const string RoutePattern = "/mcp";

    public const string ServerInstructions =
        "Klacks workforce planning and scheduling. Tools mirror the built-in assistant skills, " +
        "filtered by the authenticated user's permissions. Write actions may return a confirmation " +
        "request instead of executing immediately; confirm them by calling the " +
        "'confirm_pending_action' tool with the provided confirmation token.";
}
