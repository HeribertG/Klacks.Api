// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>Parameters of ForbiddenTransition: no ToKind day within WithinDays days after a FromKind day.</summary>
/// <param name="FromKind">Kind of the earlier day</param>
/// <param name="ToKind">Kind that must not follow</param>
/// <param name="WithinDays">Number of following days the ToKind is forbidden on (at least 1)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record ForbiddenTransitionParameters(PlanningShiftKind FromKind, PlanningShiftKind ToKind, int WithinDays) : PlanningConstraintParameters
{
    public override PlanningConstraintKind Kind => PlanningConstraintKind.ForbiddenTransition;
}
