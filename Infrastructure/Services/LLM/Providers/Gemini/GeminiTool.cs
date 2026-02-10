using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Gemini;

public class GeminiTool
{
    [JsonPropertyName("function_declarations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GeminiFunctionDeclaration>? FunctionDeclarations { get; set; }
}
