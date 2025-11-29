using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiFunctionDeclaration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiFunctionParameters? Parameters { get; set; }
}
