using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}