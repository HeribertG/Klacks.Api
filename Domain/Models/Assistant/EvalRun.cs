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

    /// <summary>
    /// Version of the scoring rules that produced <see cref="CompositeScore"/>. Runs with different
    /// versions are not comparable; the baseline lookup therefore filters on it. Historical rows
    /// carry the migration default 1.
    /// </summary>
    public int ScorerVersion { get; set; } = 1;

    /// <summary>
    /// True when the run covered only a subset of its goldset (maxItems, or a caller-trimmed item
    /// list). Partial runs are reported like any other run but are never used as a baseline.
    /// </summary>
    public bool IsPartial { get; set; }
}
