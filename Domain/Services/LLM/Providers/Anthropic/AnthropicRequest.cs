namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicRequest
{
    public string Model { get; set; } = string.Empty;
    public List<AnthropicMessage> Messages { get; set; } = new();
    public string? System { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public List<AnthropicTool>? Tools { get; set; }
}