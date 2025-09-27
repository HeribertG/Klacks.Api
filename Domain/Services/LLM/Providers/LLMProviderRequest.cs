using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers;

public class LLMProviderRequest
{
    public string Message { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public List<LLMMessage> ConversationHistory { get; set; } = new();
    public List<LLMFunction> AvailableFunctions { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
}