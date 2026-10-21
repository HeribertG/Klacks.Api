// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised when a skill fails while executing; carries the error code EXECUTION_ERROR.
/// </summary>
/// <param name="skillName">Name of the skill that failed</param>
/// <param name="message">Description of the failure</param>
/// <param name="innerException">Underlying exception, if any</param>

namespace Klacks.Api.Domain.Exceptions;

public class SkillExecutionException : SkillException
{
    public SkillExecutionException(string skillName, string message, Exception? innerException = null)
        : base(skillName, message, innerException!, "EXECUTION_ERROR")
    {
    }
}
