using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Presentation.DTOs.LLM;

public class LLMRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? ConversationId { get; set; }
}