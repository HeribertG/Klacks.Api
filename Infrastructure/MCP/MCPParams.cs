// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("arguments")]
    public System.Text.Json.JsonElement? Arguments { get; set; }
    
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}