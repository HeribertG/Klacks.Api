// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillUsageSummaryDto
{
    public required string SkillName { get; init; }
    public int ExecutionCount { get; init; }
    public decimal SuccessRate { get; init; }
    public double AvgDurationMs { get; init; }
}
