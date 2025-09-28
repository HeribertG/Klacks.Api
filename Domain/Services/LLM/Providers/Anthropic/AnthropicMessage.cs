using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}