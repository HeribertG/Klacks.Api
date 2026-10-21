// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised when the parameters passed to a skill fail validation; carries the error code VALIDATION_ERROR.
/// </summary>
/// <param name="skillName">Name of the skill whose input was rejected</param>
/// <param name="validationErrors">One message per failed validation rule</param>

namespace Klacks.Api.Domain.Exceptions;

public class SkillValidationException : SkillException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public SkillValidationException(string skillName, IReadOnlyList<string> validationErrors)
        : base(skillName, $"Validation failed: {string.Join(", ", validationErrors)}", "VALIDATION_ERROR")
    {
        ValidationErrors = validationErrors;
    }
}
