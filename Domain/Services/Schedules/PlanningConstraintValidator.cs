// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningConstraintValidator"/>. Checks the defined enums, the scope shape (Global
/// without ScopeId, every other scope with one), the validity range, the weight (finite, 0..MaxWeight,
/// positive for Soft), the text lengths, the per-kind ParametersJson schema and the owner decision of
/// 2026-10-03: TeamFairness is Soft only and must be scoped to a Group.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Services.Schedules;

public sealed class PlanningConstraintValidator : IPlanningConstraintValidator
{
    public PlanningConstraintValidationResult Validate(PlanningConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        var errors = new List<string>();

        if (!Enum.IsDefined(constraint.Kind))
        {
            errors.Add("Kind must be a defined planning constraint kind.");
            return new PlanningConstraintValidationResult(errors, null);
        }

        ValidateSeverityAndWeight(constraint, errors);
        ValidateScope(constraint, errors);
        ValidateValidity(constraint, errors);
        ValidateTexts(constraint, errors);
        ValidateTeamFairnessRules(constraint, errors);

        var parameters = PlanningConstraintParametersParser.Parse(constraint.Kind, constraint.ParametersJson, errors);
        return new PlanningConstraintValidationResult(errors, parameters);
    }

    public PlanningConstraintParameters? ParseParameters(PlanningConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        return PlanningConstraintParametersParser.Parse(constraint.Kind, constraint.ParametersJson, []);
    }

    private static void ValidateSeverityAndWeight(PlanningConstraint constraint, List<string> errors)
    {
        if (!Enum.IsDefined(constraint.Severity))
        {
            errors.Add("Severity must be Hard or Soft.");
        }

        if (!double.IsFinite(constraint.Weight) || constraint.Weight < 0 || constraint.Weight > PlanningConstraintDefaults.MaxWeight)
        {
            errors.Add($"Weight must be a finite number between 0 and {PlanningConstraintDefaults.MaxWeight}.");
        }
        else if (constraint.Severity == PlanningConstraintSeverity.Soft && constraint.Weight <= 0)
        {
            errors.Add("Weight must be greater than 0 for a soft constraint.");
        }
    }

    private static void ValidateScope(PlanningConstraint constraint, List<string> errors)
    {
        if (!Enum.IsDefined(constraint.ScopeType))
        {
            errors.Add("ScopeType must be Global, SchedulingRule, Group or Client.");
            return;
        }

        if (constraint.ScopeType == PlanningConstraintScopeType.Global && constraint.ScopeId.HasValue)
        {
            errors.Add("ScopeId must be empty for a global constraint.");
        }

        if (constraint.ScopeType != PlanningConstraintScopeType.Global
            && (!constraint.ScopeId.HasValue || constraint.ScopeId.Value == Guid.Empty))
        {
            errors.Add("ScopeId is required unless the scope is global.");
        }
    }

    private static void ValidateValidity(PlanningConstraint constraint, List<string> errors)
    {
        if (constraint.ValidFrom.HasValue && constraint.ValidUntil.HasValue && constraint.ValidUntil < constraint.ValidFrom)
        {
            errors.Add("ValidUntil must not be before ValidFrom.");
        }
    }

    private static void ValidateTexts(PlanningConstraint constraint, List<string> errors)
    {
        if (constraint.SourceText?.Length > PlanningConstraintDefaults.SourceTextMaxLength)
        {
            errors.Add($"SourceText must not exceed {PlanningConstraintDefaults.SourceTextMaxLength} characters.");
        }

        if (constraint.Paraphrase?.Length > PlanningConstraintDefaults.ParaphraseMaxLength)
        {
            errors.Add($"Paraphrase must not exceed {PlanningConstraintDefaults.ParaphraseMaxLength} characters.");
        }
    }

    private static void ValidateTeamFairnessRules(PlanningConstraint constraint, List<string> errors)
    {
        if (constraint.Kind != PlanningConstraintKind.TeamFairness)
        {
            return;
        }

        if (constraint.Severity == PlanningConstraintSeverity.Hard)
        {
            errors.Add("TeamFairness can only be a soft constraint.");
        }

        if (constraint.ScopeType != PlanningConstraintScopeType.Group)
        {
            errors.Add("TeamFairness must be scoped to a group.");
        }
    }
}
