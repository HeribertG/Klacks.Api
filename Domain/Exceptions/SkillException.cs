// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Exceptions;

public class SkillException : Exception
{
    public string SkillName { get; }
    public string? ErrorCode { get; }

    public SkillException(string skillName, string message, string? errorCode = null)
        : base(message)
    {
        SkillName = skillName;
        ErrorCode = errorCode;
    }

    public SkillException(string skillName, string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        SkillName = skillName;
        ErrorCode = errorCode;
    }
}

public class SkillNotFoundException : SkillException
{
    public SkillNotFoundException(string skillName)
        : base(skillName, $"Skill '{skillName}' not found", "SKILL_NOT_FOUND")
    {
    }
}

public class SkillPermissionException : SkillException
{
    public IReadOnlyList<string> MissingPermissions { get; }

    public SkillPermissionException(string skillName, IReadOnlyList<string> missingPermissions)
        : base(skillName, $"Permission denied. Missing permissions: {string.Join(", ", missingPermissions)}", "PERMISSION_DENIED")
    {
        MissingPermissions = missingPermissions;
    }
}

public class SkillValidationException : SkillException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public SkillValidationException(string skillName, IReadOnlyList<string> validationErrors)
        : base(skillName, $"Validation failed: {string.Join(", ", validationErrors)}", "VALIDATION_ERROR")
    {
        ValidationErrors = validationErrors;
    }
}

public class SkillExecutionException : SkillException
{
    public SkillExecutionException(string skillName, string message, Exception? innerException = null)
        : base(skillName, message, innerException!, "EXECUTION_ERROR")
    {
    }
}
