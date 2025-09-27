namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}