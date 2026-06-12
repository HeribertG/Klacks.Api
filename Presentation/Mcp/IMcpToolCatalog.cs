// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpToolCatalog
{
    IList<Tool> GetToolsForUser(IReadOnlyList<string> userPermissions);
}
