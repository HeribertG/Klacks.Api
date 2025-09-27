namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiGenerationConfig
{
    public double Temperature { get; set; }

    public int MaxOutputTokens { get; set; }
}