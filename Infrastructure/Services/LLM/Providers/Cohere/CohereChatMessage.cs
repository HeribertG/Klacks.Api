namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Cohere;

public class CohereChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}