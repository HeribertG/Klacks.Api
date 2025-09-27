namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; } = new();

    public GeminiGenerationConfig? GenerationConfig { get; set; }
}