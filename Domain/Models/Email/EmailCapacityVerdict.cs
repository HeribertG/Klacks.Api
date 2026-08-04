// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of judging an absence request from an email against the staffing reserve.
/// </summary>
/// <param name="Evaluated">False when capacity could not be judged at all, for example when the employee's group is not unambiguous</param>
/// <param name="HasGap">True when at least one time window would exceed the utilization ceiling</param>
/// <param name="Note">Human-readable summary for the planner notification; empty when nothing could be judged</param>

namespace Klacks.Api.Domain.Models.Email;

public sealed record EmailCapacityVerdict(bool Evaluated, bool HasGap, string Note)
{
    public static EmailCapacityVerdict NotEvaluated() => new(false, false, string.Empty);
}
