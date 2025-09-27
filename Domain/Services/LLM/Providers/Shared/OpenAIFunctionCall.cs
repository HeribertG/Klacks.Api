namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIFunctionCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}