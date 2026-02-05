using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.LLM;

public class LLMRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? ConversationId { get; set; }
    
    public string? ModelId { get; set; }
    
    public string? Language { get; set; }
    
    public object? Context { get; set; }
}