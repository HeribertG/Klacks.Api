// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Presentation.Mcp;

public record McpUserContext(
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> Permissions);
