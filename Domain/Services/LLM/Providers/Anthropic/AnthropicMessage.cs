namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}