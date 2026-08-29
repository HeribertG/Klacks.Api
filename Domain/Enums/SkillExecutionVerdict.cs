// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SkillExecutionVerdict
{
    /// <summary>
    /// The composition is sound and everything that could be run without side effects did run and worked.
    /// </summary>
    Passed = 0,

    /// <summary>
    /// The composition is demonstrably unusable. This is a verdict about the candidate and costs it an
    /// attempt.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// The oracle could not form a verdict because something outside the candidate was unavailable - no
    /// owner identity, a refused token. Never a reason to reject a wish: the candidate is untested, not
    /// disproved.
    /// </summary>
    Inconclusive = 2
}
