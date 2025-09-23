using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.LLM;

public class LLMModel : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ModelId { get; set; } = string.Empty; // gpt-5, claude-37-opus, etc.
    
    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty; // Display name
    
    [Required]
    [MaxLength(50)]
    public string ApiModelId { get; set; } = string.Empty; // Actual API model identifier
    
    [Required]
    public Guid ProviderId { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public bool IsDefault { get; set; }
    
    [Column(TypeName = "decimal(10, 6)")]
    public decimal CostPerInputToken { get; set; } // Cost per 1000 tokens
    
    [Column(TypeName = "decimal(10, 6)")]
    public decimal CostPerOutputToken { get; set; } // Cost per 1000 tokens
    
    public int MaxTokens { get; set; } = 4096;
    
    public int ContextWindow { get; set; } = 128000;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; } // fast, balanced, powerful
    
    public DateTime? ReleasedAt { get; set; }
    
    public DateTime? DeprecatedAt { get; set; }
    
    // Navigation
    [ForeignKey("ProviderId")]
    public virtual LLMProvider Provider { get; set; } = null!;
    
    public virtual ICollection<LLMUsage> Usages { get; set; } = new List<LLMUsage>();
}