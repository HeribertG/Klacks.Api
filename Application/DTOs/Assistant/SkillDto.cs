using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required SkillCategory Category { get; init; }
    public required IReadOnlyList<SkillParameterDto> Parameters { get; init; }
    public required IReadOnlyList<string> RequiredPermissions { get; init; }
}

public record SkillParameterDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required SkillParameterType Type { get; init; }
    public required bool Required { get; init; }
    public object? DefaultValue { get; init; }
    public IReadOnlyList<string>? EnumValues { get; init; }
}
