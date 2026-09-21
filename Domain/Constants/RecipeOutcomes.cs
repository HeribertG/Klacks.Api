// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What became of the recipe confirmation gate a turn opened. Written on the trajectory of the turn that
/// asked the question: <c>pending</c> the moment the question is asked, then resolved by the following
/// turn to <c>declined</c> (the user answered with a bare negation), <c>redirected</c> (the reply neither
/// affirmed nor merely refused, so the gate was abandoned in favour of another topic) or
/// <c>confirmed</c> (the next turn resumed the same recipe). Null for every turn that never opened a gate.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class RecipeOutcomes
{
    public const string Pending = "pending";
    public const string Declined = "declined";
    public const string Confirmed = "confirmed";

    /// <summary>
    /// The gate was abandoned by a reply that carried its own content instead of a plain refusal. Kept
    /// apart from <see cref="Declined"/> because such a turn is no verdict on the recipe's trigger and
    /// must therefore stay out of the decline learning signal.
    /// </summary>
    public const string Redirected = "redirected";

    public const int MaxLength = 16;
}
