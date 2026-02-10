using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Gemini;

public class GeminiFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Args { get; set; }
}