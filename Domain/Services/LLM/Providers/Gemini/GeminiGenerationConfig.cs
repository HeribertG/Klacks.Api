using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }
}