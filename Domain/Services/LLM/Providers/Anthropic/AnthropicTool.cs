namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AnthropicInputSchema InputSchema { get; set; } = new();
}