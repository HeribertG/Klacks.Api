namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIFunction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OpenAIFunctionParameters Parameters { get; set; } = new();
}