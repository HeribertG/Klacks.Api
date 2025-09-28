using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
    
    [JsonPropertyName("functions")]
    public List<OpenAIFunction>? Functions { get; set; }
    
    [JsonPropertyName("function_call")]
    public string? FunctionCall { get; set; }
}