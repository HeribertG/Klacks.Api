// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>Parameters of MaxConsecutiveOfKind: at most MaxRun consecutive days of ShiftKind.</summary>
/// <param name="ShiftKind">Day kind the run is made of</param>
/// <param name="MaxRun">Longest allowed run (at least 1)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record MaxConsecutiveOfKindParameters(PlanningShiftKind ShiftKind, int MaxRun) : PlanningConstraintParameters
{
    public override PlanningConstraintKind Kind => PlanningConstraintKind.MaxConsecutiveOfKind;
}
