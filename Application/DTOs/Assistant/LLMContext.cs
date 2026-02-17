namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMContext
{
    public string Message { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    public List<string> UserRights { get; set; } = new();
    
    public List<LLMFunction> AvailableFunctions { get; set; } = new();
    
    public string? ConversationId { get; set; }
    
    public string? ModelId { get; set; }
    
    public string? Language { get; set; }
}