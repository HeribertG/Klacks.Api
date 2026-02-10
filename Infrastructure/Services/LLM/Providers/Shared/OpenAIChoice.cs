namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Shared;

public class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}