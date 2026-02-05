using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Skills;

public record SkillExecuteResponse
{
    public required bool Success { get; init; }
    public object? Data { get; init; }
    public string? Message { get; init; }
    public required SkillResultType ResultType { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
