using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
    
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}