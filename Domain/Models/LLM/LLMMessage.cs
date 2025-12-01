using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.LLM;

public class LLMMessage : BaseEntity
{
    [Required]
    public Guid ConversationId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty; // user, assistant, system
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public int? TokenCount { get; set; }
    
    [MaxLength(50)]
    public string? ModelId { get; set; }
    
    [MaxLength(500)]
    public string? FunctionCalls { get; set; } // JSON
    
    // Navigation
    [JsonIgnore]
    [ForeignKey("ConversationId")]
    public virtual LLMConversation Conversation { get; set; } = null!;
}