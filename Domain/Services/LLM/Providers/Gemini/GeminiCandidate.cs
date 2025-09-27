namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }

    public string? FinishReason { get; set; }
}