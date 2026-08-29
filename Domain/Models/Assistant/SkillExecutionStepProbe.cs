// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What oracle O2 found out about one step of a composed capability.
/// </summary>
/// <param name="Skill">Name of the skill the step runs</param>
/// <param name="RiskClass">How the risk classifier rated that skill</param>
/// <param name="Executed">False when the step was only checked statically, which is the normal outcome for anything that writes</param>
/// <param name="Success">Whether the step succeeded; meaningless while Executed is false</param>
/// <param name="DurationMs">Wall-clock time of the execution, zero when it was not executed</param>
/// <param name="Error">Why the step failed or was not executed</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillExecutionStepProbe(
    string Skill,
    string RiskClass,
    bool Executed,
    bool Success,
    long DurationMs,
    string? Error);
