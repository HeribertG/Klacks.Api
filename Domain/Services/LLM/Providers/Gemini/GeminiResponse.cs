namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiResponse
{
    public List<GeminiCandidate> Candidates { get; set; } = new();

    public GeminiUsageMetadata? UsageMetadata { get; set; }
}