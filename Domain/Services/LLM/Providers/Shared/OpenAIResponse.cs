namespace Klacks.Api.Domain.Services.LLM.Providers.Shared;

public class OpenAIResponse
{
    public List<OpenAIChoice> Choices { get; set; } = new();
    public OpenAIUsage? Usage { get; set; }
}