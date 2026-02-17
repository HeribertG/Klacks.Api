namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillExecuteRequest
{
    public required string SkillName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
}
