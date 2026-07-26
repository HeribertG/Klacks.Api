// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMModel : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ModelId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ApiModelId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; }
    
    public bool IsDefault { get; set; }
    
    [Column(TypeName = "decimal(10, 6)")]
    public decimal CostPerInputToken { get; set; } // Cost per 1000 tokens
    
    [Column(TypeName = "decimal(10, 6)")]
    public decimal CostPerOutputToken { get; set; } // Cost per 1000 tokens

    /// <summary>
    /// Cost per 1000 prompt tokens written into the provider's prompt cache. Null means the provider
    /// falls back to its documented multiple of <see cref="CostPerInputToken"/>, which also depends on
    /// the requested cache TTL — set this explicitly once a model's rate is known.
    /// </summary>
    [Column(TypeName = "decimal(10, 6)")]
    public decimal? CostPerCacheWriteToken { get; set; }

    /// <summary>
    /// Cost per 1000 prompt tokens served from the provider's prompt cache. Null means the provider
    /// falls back to its documented multiple of <see cref="CostPerInputToken"/>.
    /// </summary>
    [Column(TypeName = "decimal(10, 6)")]
    public decimal? CostPerCacheReadToken { get; set; }


    public int MaxTokens { get; set; } = 4096;
    
    public int ContextWindow { get; set; } = 128000;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public DateTime? ReleasedAt { get; set; }
    
    public DateTime? DeprecatedAt { get; set; }
    
    public virtual ICollection<LLMUsage> Usages { get; set; } = new List<LLMUsage>();
}