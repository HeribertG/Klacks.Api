// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Mcp;

public record McpUserContext(
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> Permissions);
