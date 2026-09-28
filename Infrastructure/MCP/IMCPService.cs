// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.MCP;

public interface IMCPService
{
    Task<bool> ConnectAsync();
    Task<List<MCPTool>> GetAvailableToolsAsync();
    Task<string> ExecuteToolAsync(string toolName, object arguments);
    Task<List<MCPResource>> GetAvailableResourcesAsync();
    Task<string> ReadResourceAsync(string uri);
    Task DisconnectAsync();
}