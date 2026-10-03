// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// An approved HARD planning constraint cannot be evaluated (its stored parameters fail validation). Loading
/// the rule set fails closed instead of planning without a binding rule; an admin has to fix or revoke the row.
/// </summary>
/// <param name="constraintId">Id of the invalid constraint</param>
/// <param name="errors">Validation errors of the stored row</param>

namespace Klacks.Api.Domain.Exceptions;

public class PlanningRuleConfigurationException : Exception
{
    public PlanningRuleConfigurationException(Guid constraintId, string errors)
        : base($"Approved hard planning constraint {constraintId} is invalid and blocks planning until it is fixed or revoked: {errors}")
    {
        ConstraintId = constraintId;
    }

    public const string ErrorCode = "planningRuleConfigurationInvalid";

    public Guid ConstraintId { get; }
}
