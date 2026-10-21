// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised when a skill ran but its result could not be verified; carries the error code VERIFICATION_FAILED.
/// </summary>
/// <param name="skillName">Name of the skill whose result failed verification</param>
/// <param name="message">Description of the verification failure</param>

namespace Klacks.Api.Domain.Exceptions;

public class SkillVerificationException : SkillException
{
    public SkillVerificationException(string skillName, string message)
        : base(skillName, message, "VERIFICATION_FAILED")
    {
    }
}
