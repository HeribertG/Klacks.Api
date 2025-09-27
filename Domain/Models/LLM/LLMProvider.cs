using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.LLM;

public class LLMProvider : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty; 
    
    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ApiKey { get; set; } 
    
    public bool IsEnabled { get; set; }
    
    [MaxLength(200)]
    public string? BaseUrl { get; set; } 
    
    [MaxLength(50)]
    public string? ApiVersion { get; set; }
    
    public int Priority { get; set; } 
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Settings { get; set; }
    
    public virtual ICollection<LLMModel> Models { get; set; } = new List<LLMModel>();
}