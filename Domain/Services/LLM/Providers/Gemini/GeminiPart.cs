using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("functionCall")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiFunctionCall? FunctionCall { get; set; }
}