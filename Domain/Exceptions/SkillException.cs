// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
