// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a mutation on a learning artefact, expressed so the controller can answer without knowing
/// the domain: not found is 404, a conflict is 409, an error is 400, and success is 204.
/// </summary>
namespace Klacks.Api.Application.Commands.Assistant.Learning;

public sealed record LearningMutationResult(bool Found, bool Conflict, string? Error)
{
    public static LearningMutationResult Success() => new(true, false, null);

    public static LearningMutationResult NotFound() => new(false, false, null);

    public static LearningMutationResult Duplicate() => new(true, true, "A phrase with this text already exists for this skill and language.");

    /// <summary>
    /// The row was withdrawn, but the description it had applied could not be put back because something
    /// else has changed it since. Reported as a conflict rather than as plain success: the card would
    /// otherwise claim the change was undone while a foreign description stays live.
    /// </summary>
    public static LearningMutationResult StaleDescription() => new(
        true,
        true,
        "The proposal was rejected, but the description was changed by someone else in the meantime and "
            + "was therefore left as it is.");

    public static LearningMutationResult Invalid(string error) => new(true, false, error);
}
