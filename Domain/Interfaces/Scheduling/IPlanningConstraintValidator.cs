// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a PlanningConstraint row before it is stored or approved: kind/severity/scope consistency,
/// the per-kind ParametersJson schema, the validity range, and the owner rule that TeamFairness is soft only
/// and group-scoped only.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IPlanningConstraintValidator
{
    PlanningConstraintValidationResult Validate(PlanningConstraint constraint);

    PlanningConstraintParameters? ParseParameters(PlanningConstraint constraint);
}
