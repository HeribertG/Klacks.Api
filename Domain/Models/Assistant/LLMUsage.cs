using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMUsage : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public Guid ModelId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ConversationId { get; set; } = string.Empty;
    
    public int InputTokens { get; set; }
    
    public int OutputTokens { get; set; }
    
    public int TotalTokens => InputTokens + OutputTokens;
    
    [Column(TypeName = "decimal(10, 4)")]
    public decimal Cost { get; set; }
    
    [MaxLength(4000)]
    public string? UserMessage { get; set; }
    
    [MaxLength(4000)]
    public string? AssistantMessage { get; set; }
    
    public int ResponseTimeMs { get; set; }
    
    public bool HasError { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(200)]
    public string? FunctionsCalled { get; set; } // JSON array of function names
    
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } = null!;
    
    [ForeignKey("ModelId")]
    public virtual LLMModel Model { get; set; } = null!;
}