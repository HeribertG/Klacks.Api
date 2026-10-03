// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>Parameters of RestAfterKind: FreeDays free days after the last day of a ShiftKind block.</summary>
/// <param name="ShiftKind">Kind of the block</param>
/// <param name="FreeDays">Free days required after the block (at least 1)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record RestAfterKindParameters(PlanningShiftKind ShiftKind, int FreeDays) : PlanningConstraintParameters
{
    public override PlanningConstraintKind Kind => PlanningConstraintKind.RestAfterKind;
}
