namespace Klacks.Api.Application.DTOs.Skills;

public record SkillExecuteRequest
{
    public required string SkillName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
}
