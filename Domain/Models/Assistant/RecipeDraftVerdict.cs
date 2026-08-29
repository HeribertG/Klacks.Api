// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of validating a generated capability. Carries the final recipe name and the trigger the
/// validator hardened, so the learner writes exactly what was judged and not what the generator sent.
/// </summary>
/// <param name="Name">Prefixed recipe name, null when the draft was rejected</param>
/// <param name="Trigger">Trigger including the question guard the validator added, null when rejected</param>
/// <param name="Error">Why the draft was rejected, null when it was accepted</param>
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record RecipeDraftVerdict(string? Name, RecipeTrigger? Trigger, string? Error)
{
    public bool IsAccepted => Error == null;

    public static RecipeDraftVerdict Accepted(string name, RecipeTrigger trigger) => new(name, trigger, null);

    public static RecipeDraftVerdict Rejected(string error) => new(null, null, error);
}
