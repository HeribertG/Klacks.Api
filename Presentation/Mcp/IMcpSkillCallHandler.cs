// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
