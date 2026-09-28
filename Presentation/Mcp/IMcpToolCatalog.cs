// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpToolCatalog
{
    IList<Tool> GetToolsForUser(IReadOnlyList<string> userPermissions);
}
