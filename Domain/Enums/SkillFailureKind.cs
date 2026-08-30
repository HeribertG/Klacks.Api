// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Failure classes for skill invocations that never reached a dispatch (W1.2). Persisted on
/// skill_usage_records so the hallucination rate, permission denials, parameter errors, gate holds,
/// missing UI context and exceptions are countable per SQL instead of only visible in logs.
/// </summary>
public enum SkillFailureKind
{
    /// <summary>The model called a skill name that does not exist in the registry (hallucination).</summary>
    NotFound = 1,

    /// <summary>The user lacks one of the skill's required permissions.</summary>
    PermissionDenied = 2,

    /// <summary>Required parameters are missing or values fail the declared type/enum validation.</summary>
    ParameterInvalid = 3,

    /// <summary>The autonomy gate held the execution and asked for confirmation instead.</summary>
    GateHold = 4,

    /// <summary>A UiAction was called in a context without an interactive UI session.</summary>
    UiActionContext = 5,

    /// <summary>Any exception or configuration error (no implementation/handler) during execution.</summary>
    Exception = 6
}
