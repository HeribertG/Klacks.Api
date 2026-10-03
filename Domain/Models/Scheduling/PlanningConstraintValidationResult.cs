// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of validating a PlanningConstraint: the error messages (English, empty when valid) and, when the
/// ParametersJson parsed, its typed parameters.
/// </summary>
/// <param name="Errors">Validation errors; empty means valid</param>
/// <param name="Parameters">Typed parameters, null when ParametersJson did not parse</param>

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record PlanningConstraintValidationResult(IReadOnlyList<string> Errors, PlanningConstraintParameters? Parameters)
{
    public bool IsValid => Errors.Count == 0 && Parameters is not null;
}
