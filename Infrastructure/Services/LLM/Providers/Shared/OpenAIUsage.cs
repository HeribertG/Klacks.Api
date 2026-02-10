namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Shared;

public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}