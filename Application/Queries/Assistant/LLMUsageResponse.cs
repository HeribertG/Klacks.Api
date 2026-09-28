// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Queries.Assistant;

public class LLMUsageResponse
{
    public int TotalTokens { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dictionary<string, decimal> ModelUsage { get; set; } = new();

    public List<DailyUsage> DailyUsage { get; set; } = new();
}