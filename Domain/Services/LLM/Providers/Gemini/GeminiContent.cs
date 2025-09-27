namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiContent
{
    public string? Role { get; set; }

    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}