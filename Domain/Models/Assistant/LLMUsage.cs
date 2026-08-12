// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    public int CacheCreationInputTokens { get; set; }

    public int CacheReadInputTokens { get; set; }

    public int TotalTokens => InputTokens + CacheCreationInputTokens + CacheReadInputTokens + OutputTokens;
    
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

    /// <summary>
    /// Milliseconds from turn start until the first content token reached the client (streaming
    /// path only, null elsewhere). Together with ToolsetAssemblyMs and ToolIterations this makes
    /// the latency profile of every installation diagnosable per SQL, independent of the model chosen.
    /// </summary>
    public int? TtftMs { get; set; }

    /// <summary>
    /// Milliseconds the pre-LLM toolset assembly took for this turn (skill loading, permission
    /// filter, retrieval and guarantee logic — retrieval being the dominant part).
    /// </summary>
    public int? ToolsetAssemblyMs { get; set; }

    /// <summary>
    /// Number of LLM round-trips the tool loop ran for this turn (1 = plain answer, no tools).
    /// </summary>
    public int? ToolIterations { get; set; }
    
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } = null!;
    
    [ForeignKey("ModelId")]
    public virtual LLMModel Model { get; set; } = null!;
}