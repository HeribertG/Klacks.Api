namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public List<AnthropicContent>? Content { get; set; }
    public AnthropicUsage? Usage { get; set; }
}