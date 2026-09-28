// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.MCP;

public class ResourceReadResult
{
    public List<ResourceContent> Contents { get; set; } = new();
}