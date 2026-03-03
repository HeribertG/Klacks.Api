// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Queries.Assistant;

public class LLMUsageRawData
{
    public List<Domain.Models.Assistant.LLMUsage> Usages { get; set; } = new();

    public Dictionary<string, decimal> ModelSummary { get; set; } = new();

    public decimal TotalCost { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}