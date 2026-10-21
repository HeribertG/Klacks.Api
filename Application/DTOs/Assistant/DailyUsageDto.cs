// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record DailyUsageDto
{
    public DateOnly Date { get; init; }
    public int Executions { get; init; }
    public int Successes { get; init; }
}
