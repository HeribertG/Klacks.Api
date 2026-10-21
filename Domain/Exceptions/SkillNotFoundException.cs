// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised when no skill with the requested name is registered; carries the error code SKILL_NOT_FOUND.
/// </summary>
/// <param name="skillName">Name of the skill that was requested</param>

namespace Klacks.Api.Domain.Exceptions;

public class SkillNotFoundException : SkillException
{
    public SkillNotFoundException(string skillName)
        : base(skillName, $"Skill '{skillName}' not found", "SKILL_NOT_FOUND")
    {
    }
}
