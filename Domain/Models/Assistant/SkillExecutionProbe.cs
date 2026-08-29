// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verdict of oracle O2 over one composed capability, plus the per-step record that is stored as the
/// candidate's execution result and fed back to the generator when a round has to be repeated.
/// </summary>
/// <param name="Verdict">Whether the composition passed, was rejected, or could not be judged at all</param>
/// <param name="FullyExecuted">True only when every step actually ran; false leaves the capability owing a first real use</param>
/// <param name="Steps">One entry per step, in execution order</param>
/// <param name="Error">Why the composition was rejected or could not be judged, null when it passed</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillExecutionProbe(
    SkillExecutionVerdict Verdict,
    bool FullyExecuted,
    IReadOnlyList<SkillExecutionStepProbe> Steps,
    string? Error)
{
    public static SkillExecutionProbe Rejected(string error, IReadOnlyList<SkillExecutionStepProbe> steps) =>
        new(SkillExecutionVerdict.Rejected, false, steps, error);

    public static SkillExecutionProbe Inconclusive(string reason) =>
        new(SkillExecutionVerdict.Inconclusive, false, [], reason);

    public static SkillExecutionProbe Passed(
        bool fullyExecuted, IReadOnlyList<SkillExecutionStepProbe> steps) =>
        new(SkillExecutionVerdict.Passed, fullyExecuted, steps, null);
}
