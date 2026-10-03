// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Presentation.Mcp;

public record McpUserContext(
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> Permissions,
    PersonalAccessTokenAccessMode AccessMode);
