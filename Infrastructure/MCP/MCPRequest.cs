using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public System.Text.Json.JsonElement? Params { get; set; }
}