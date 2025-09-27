namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiUsageMetadata
{
    public int PromptTokenCount { get; set; }

    public int CandidatesTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}