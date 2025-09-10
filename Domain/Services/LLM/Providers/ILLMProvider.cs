using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    
    Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}

public class LLMProviderRequest
{
    public string Message { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public List<LLMMessage> ConversationHistory { get; set; } = new();
    public List<LLMFunction> AvailableFunctions { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

public class LLMProviderResponse
{
    public string Content { get; set; } = string.Empty;
    public List<LLMFunctionCall> FunctionCalls { get; set; } = new();
    public LLMUsage Usage { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
}

public class LLMMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class LLMFunctionCall
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Result { get; set; }
}

public class LLMUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal Cost { get; set; }
}