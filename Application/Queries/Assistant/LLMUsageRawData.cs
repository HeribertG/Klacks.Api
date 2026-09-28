// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Queries.Assistant;

public class LLMUsageRawData
{
    public List<Domain.Models.Assistant.LLMUsage> Usages { get; set; } = new();

    public Dictionary<string, decimal> ModelSummary { get; set; } = new();

    public decimal TotalCost { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}