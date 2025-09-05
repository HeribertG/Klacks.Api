namespace Klacks.Api.Presentation.DTOs.LLM;

public class LLMContext
{
    public string Message { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    
    public List<string> UserRights { get; set; } = new();
    
    public List<LLMFunction> AvailableFunctions { get; set; } = new();
    
    public string? ConversationId { get; set; }
}