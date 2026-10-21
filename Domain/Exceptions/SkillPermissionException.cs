// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised when the caller lacks permissions a skill requires; carries the error code PERMISSION_DENIED.
/// </summary>
/// <param name="skillName">Name of the skill that was denied</param>
/// <param name="missingPermissions">Permissions the caller would need in addition</param>

namespace Klacks.Api.Domain.Exceptions;

public class SkillPermissionException : SkillException
{
    public IReadOnlyList<string> MissingPermissions { get; }

    public SkillPermissionException(string skillName, IReadOnlyList<string> missingPermissions)
        : base(skillName, $"Permission denied. Missing permissions: {string.Join(", ", missingPermissions)}", "PERMISSION_DENIED")
    {
        MissingPermissions = missingPermissions;
    }
}
