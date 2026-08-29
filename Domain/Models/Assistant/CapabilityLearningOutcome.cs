// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one round of capability learning produced. Distinguishes three outcomes rather than two,
/// because "no usable composition exists" and "the round could not be judged" must not cost the same:
/// only the first is evidence about the wish, and only the first may spend an attempt.
/// </summary>
/// <param name="RecipeName">Name of the activated recipe, null when nothing was activated</param>
/// <param name="NeedsFirstUse">True when the execution oracle could not run every step, so the first real use is still owed</param>
/// <param name="Error">Why the round produced nothing, null on success</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record CapabilityLearningOutcome(
    bool Learned, bool Inconclusive, string? RecipeName, bool NeedsFirstUse, string? Error)
{
    public static CapabilityLearningOutcome Success(string recipeName, bool needsFirstUse) =>
        new(true, false, recipeName, needsFirstUse, null);

    public static CapabilityLearningOutcome Failure(string error) => new(false, false, null, false, error);

    public static CapabilityLearningOutcome Unjudged(string reason) => new(false, true, null, false, reason);
}
