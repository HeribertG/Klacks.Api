namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIResponse
{
    public List<OpenAIChoice> Choices { get; set; } = new();
    public OpenAIUsage? Usage { get; set; }
}