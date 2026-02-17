namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}