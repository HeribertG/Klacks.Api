namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiFunctionCall
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object>? Args { get; set; }
}