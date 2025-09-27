namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicContent
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, object>? Input { get; set; }
}