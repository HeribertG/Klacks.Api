using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiGenerationConfig? GenerationConfig { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GeminiTool>? Tools { get; set; }

    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiContent? SystemInstruction { get; set; }
}
