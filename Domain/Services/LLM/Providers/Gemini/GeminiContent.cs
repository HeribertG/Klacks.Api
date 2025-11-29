using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiContent
{
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }

    [JsonPropertyName("parts")]
    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}