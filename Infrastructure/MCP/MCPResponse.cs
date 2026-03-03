// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("result")]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    public MCPError? Error { get; set; }
}