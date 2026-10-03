// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Family of a PlanningConstraint; selects the schema of its ParametersJson and the evaluator rule it maps to.</summary>
public enum PlanningConstraintKind
{
    MaxConsecutiveOfKind = 1,
    ForbiddenTransition = 2,
    RestAfterKind = 3,
    TeamFairness = 4,
}
