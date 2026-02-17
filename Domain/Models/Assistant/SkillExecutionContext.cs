using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillExecutionContext
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string UserName { get; init; }
    public required IReadOnlyList<string> UserPermissions { get; init; }
    public string? CurrentPage { get; init; }
    public IReadOnlyList<Guid>? SelectedEntityIds { get; init; }
    public string? UserTimezone { get; init; }
    public LLMProviderType? ProviderId { get; init; }
    public string? ModelId { get; init; }
}
