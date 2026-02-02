using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Skills;

public class SkillUsageRecord : BaseEntity
{
    public required string SkillName { get; set; }
    public required SkillCategory Category { get; set; }
    public required Guid UserId { get; set; }
    public required Guid TenantId { get; set; }
    public LLMProviderType? ProviderId { get; set; }
    public string? ModelId { get; set; }
    public string? ParametersJson { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}
