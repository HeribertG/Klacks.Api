using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Presentation.DTOs.Skills;

public record SkillAnalyticsDto
{
    public int TotalExecutions { get; init; }
    public decimal SuccessRate { get; init; }
    public required IReadOnlyList<SkillUsageSummaryDto> MostUsedSkills { get; init; }
    public required Dictionary<string, int> UsageByCategory { get; init; }
    public required IReadOnlyList<DailyUsageDto> UsageOverTime { get; init; }
}

public record SkillUsageSummaryDto
{
    public required string SkillName { get; init; }
    public int ExecutionCount { get; init; }
    public decimal SuccessRate { get; init; }
    public double AvgDurationMs { get; init; }
}

public record DailyUsageDto
{
    public DateOnly Date { get; init; }
    public int Executions { get; init; }
    public int Successes { get; init; }
}
