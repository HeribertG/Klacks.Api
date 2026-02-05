namespace Klacks.Api.Application.DTOs.Skills;

public record SkillChainExecuteRequest
{
    public required IReadOnlyList<SkillInvocationDto> Invocations { get; init; }
}

public record SkillInvocationDto
{
    public required string SkillName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public bool StopOnError { get; init; } = true;
}
