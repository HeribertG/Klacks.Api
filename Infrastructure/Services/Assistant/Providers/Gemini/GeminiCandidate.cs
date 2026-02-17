namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }

    public string? FinishReason { get; set; }
}