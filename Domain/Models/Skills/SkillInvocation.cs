namespace Klacks.Api.Domain.Models.Skills;

public record SkillInvocation
{
    public required string SkillName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public bool StopOnError { get; init; } = true;
}
