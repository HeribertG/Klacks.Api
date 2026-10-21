// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillAnalyticsDto
{
    public int TotalExecutions { get; init; }
    public decimal SuccessRate { get; init; }
    public required IReadOnlyList<SkillUsageSummaryDto> MostUsedSkills { get; init; }
    public required Dictionary<string, int> UsageByCategory { get; init; }
    public required IReadOnlyList<DailyUsageDto> UsageOverTime { get; init; }
}
