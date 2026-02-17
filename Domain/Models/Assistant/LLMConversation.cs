using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMConversation : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string ConversationId { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [MaxLength(500)]
    public string? Summary { get; set; }
    
    public DateTime LastMessageAt { get; set; }
    
    public int MessageCount { get; set; }
    
    public int TotalTokens { get; set; }
    
    [Column(TypeName = "decimal(10, 4)")]
    public decimal TotalCost { get; set; }
    
    [MaxLength(50)]
    public string? LastModelId { get; set; }
    
    public bool IsArchived { get; set; }
    
    // Navigation
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } = null!;
    
    public virtual ICollection<LLMMessage> Messages { get; set; } = new List<LLMMessage>();
}