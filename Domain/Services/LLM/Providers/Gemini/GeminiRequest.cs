using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}
