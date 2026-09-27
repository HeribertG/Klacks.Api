// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one sharpener run did. Applied and Blocked describe the gate's own verdicts; OptimizerAttempts and
/// OptimizerFailures are carried through unchanged from the optimizer call the run made first, so a caller can
/// tell "the gate had nothing pending" apart from "the optimizer could not turn evidence into a single usable
/// suggestion" - two situations that both leave Applied and Blocked at zero.
/// </summary>
/// <param name="Applied">Proposals that passed the gate (gate_passed or applied_auto)</param>
/// <param name="Blocked">Proposals blocked by a regression</param>
/// <param name="OptimizerAttempts">Suggestion requests the optimizer sent to the model this run</param>
/// <param name="OptimizerFailures">Of those, the ones whose answer could not be used</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillDescriptionSharpenerResult(
    int Applied, int Blocked, int OptimizerAttempts, int OptimizerFailures)
{
    public static SkillDescriptionSharpenerResult Empty { get; } = new(0, 0, 0, 0);
}
