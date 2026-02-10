namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Gemini;

public class GeminiUsageMetadata
{
    public int PromptTokenCount { get; set; }

    public int CandidatesTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}