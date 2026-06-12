// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpResourceCatalog
{
    IList<Resource> ListResources();

    Task<ReadResourceResult?> ReadResourceAsync(string uri);
}
