// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Claims;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpSkillCallHandler
{
    Task<CallToolResult> HandleAsync(
        CallToolRequestParams request,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken);
}
