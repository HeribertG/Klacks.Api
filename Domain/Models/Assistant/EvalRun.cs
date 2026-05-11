// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class EvalRun : BaseEntity
{
    public string Goldset { get; set; } = string.Empty;

    public string? Provider { get; set; }

    public string? Model { get; set; }

    public decimal CompositeScore { get; set; }

    public string DimensionsJson { get; set; } = "{}";

    public decimal? RegressionVsBaseline { get; set; }

    public int ItemsTotal { get; set; }

    public int ItemsPassed { get; set; }

    public int DurationMs { get; set; }
}
