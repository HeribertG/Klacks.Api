namespace Klacks.Api.Application.DTOs.LLM;

public class LLMResponse
{
    public string Message { get; set; } = string.Empty;
    
    public bool ActionPerformed { get; set; }
    
    public string? ConversationId { get; set; }
    
    public object? Data { get; set; }
    
    public List<string>? Suggestions { get; set; }
    
    public string? NavigateTo { get; set; }
    
    public List<object>? FunctionCalls { get; set; }
    
    public LLMUsageInfo? Usage { get; set; }
}