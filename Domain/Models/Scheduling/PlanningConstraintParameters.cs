// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>Typed, validated content of a PlanningConstraint.ParametersJson; one derived record per kind.</summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public abstract record PlanningConstraintParameters
{
    public abstract PlanningConstraintKind Kind { get; }
}
