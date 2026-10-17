// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single source of truth for whether a shift requirement's minimum level is binding. The requirement's own
/// override wins; null means "inherit" the qualification's default (null is not false). The binding only matters for a
/// MANDATORY requirement: there a binding level makes an employee who holds the qualification below MinLevel ineligible
/// (in-window, or expired and also too low), a non-binding level only warns. A gap of an optional requirement always
/// warns. Exception: the wizard eligibility matrix never vetoes an employee's pre-existing assignment retroactively for
/// a binding level; that gap is treated as non-binding there (see EligibilityMatrixBuilder).
/// </summary>
/// <param name="levelBindingOverride">The requirement's override: true = binding, false = hint only, null = inherit</param>
/// <param name="qualificationIsLevelBinding">The qualification's default (Qualification.IsLevelBinding)</param>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Schedules;

public static class QualificationLevelBinding
{
    public const bool QualificationDefault = false;

    public static bool IsBinding(bool? levelBindingOverride, bool qualificationIsLevelBinding)
        => levelBindingOverride ?? qualificationIsLevelBinding;

    /// <summary>
    /// Resolves the effective binding of a requirement row. The Qualification navigation must be loaded; when it is
    /// not (or the qualification is soft-deleted) the qualification default is used.
    /// </summary>
    public static bool IsBinding(ShiftRequiredQualification requirement)
        => IsBinding(
            requirement.LevelBindingOverride,
            requirement.Qualification?.IsLevelBinding ?? QualificationDefault);
}
