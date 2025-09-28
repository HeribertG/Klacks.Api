using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("input_schema")]
    public AnthropicInputSchema InputSchema { get; set; } = new();
}