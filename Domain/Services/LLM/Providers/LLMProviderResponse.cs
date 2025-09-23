namespace Klacks.Api.Domain.Services.LLM.Providers;

public class LLMProviderResponse
{
    public string Content { get; set; } = string.Empty;
    public List<LLMFunctionCall> FunctionCalls { get; set; } = new();
    public LLMUsage Usage { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
}