namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAIMessage> Messages { get; set; } = new();
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public List<OpenAIFunction>? Functions { get; set; }
    public string? FunctionCall { get; set; }
}