// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    public const string DocsResourceUriPrefix = "klacks://docs/";
    public const string MarkdownMimeType = "text/markdown";

    public const string ReadOnlyAccessRejectionMessage =
        "This personal access token is read-only: tool '{0}' can change data and is not available. " +
        "Ask the token owner for a token with write access.";

    public const string ConfirmationRequiredInstruction =
        "Confirmation required: the action was NOT executed and cannot be confirmed through MCP, because the " +
        "server cannot tell a client agent's confirmation from the user's. Tell the user that nothing was done; " +
        "they can start the action themselves in the Klacks assistant chat.";

    public const string ServerInstructions =
        "Klacks workforce planning and scheduling. Tools mirror the built-in assistant skills, " +
        "filtered by the authenticated user's permissions. Write actions may return a confirmation " +
        "request instead of executing immediately; such an action is not carried out through MCP and cannot be " +
        "confirmed through it, so tell the user, who can start it in the Klacks assistant chat. A read-only " +
        "personal access token lists and runs read-only tools only.";
}
