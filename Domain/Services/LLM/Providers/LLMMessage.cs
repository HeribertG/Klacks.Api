namespace Klacks.Api.Domain.Services.LLM.Providers;

public class LLMMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}