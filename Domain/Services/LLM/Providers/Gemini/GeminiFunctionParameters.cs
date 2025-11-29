using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiFunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }
}
