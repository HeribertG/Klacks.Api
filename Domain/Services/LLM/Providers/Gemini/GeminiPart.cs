namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiPart
{
    public string? Text { get; set; }

    public GeminiFunctionCall? FunctionCall { get; set; }
}