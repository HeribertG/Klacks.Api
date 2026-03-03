// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new();
}

public class MCPResource
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
}