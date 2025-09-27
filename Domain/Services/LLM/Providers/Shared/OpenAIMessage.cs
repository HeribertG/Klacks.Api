namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public OpenAIFunctionCall? FunctionCall { get; set; }
}