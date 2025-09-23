using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.LLM;

public class LLMProvider : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty; // openai, anthropic, google
    
    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty; // OpenAI, Anthropic, Google Gemini
    
    [MaxLength(500)]
    public string? ApiKey { get; set; } // Encrypted
    
    public bool IsEnabled { get; set; }
    
    [MaxLength(200)]
    public string? BaseUrl { get; set; } // Custom endpoint URL if needed
    
    [MaxLength(50)]
    public string? ApiVersion { get; set; }
    
    public int Priority { get; set; } // For fallback ordering
    
    // Navigation
    public virtual ICollection<LLMModel> Models { get; set; } = new List<LLMModel>();
}