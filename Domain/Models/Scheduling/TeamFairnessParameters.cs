// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameters of the soft-only TeamFairness constraint: the spread of Metric across the group members within
/// every Window must not exceed MaxSpread.
/// </summary>
/// <param name="Metric">Compared metric</param>
/// <param name="Window">Comparison window</param>
/// <param name="MaxSpread">Tolerated spread between highest and lowest value (not negative)</param>
/// <param name="ProRata">True scales each value to full time by the workload (default true)</param>
/// <param name="WeekendDays">Weekend days for the WeekendDays metric; required for that metric, no country default</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record TeamFairnessParameters(
    PlanningFairnessMetric Metric,
    PlanningFairnessWindow Window,
    decimal MaxSpread,
    bool ProRata,
    IReadOnlySet<DayOfWeek> WeekendDays) : PlanningConstraintParameters
{
    public override PlanningConstraintKind Kind => PlanningConstraintKind.TeamFairness;
}
