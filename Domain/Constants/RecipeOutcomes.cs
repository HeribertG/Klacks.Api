// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What became of the recipe confirmation gate a turn opened. Written on the trajectory of the turn that
/// asked the question: <c>pending</c> the moment the question is asked, then resolved by the following
/// turn to <c>declined</c> (the user answered with a bare negation) or <c>confirmed</c> (the next turn
/// resumed the same recipe). Null for every turn that never opened a gate.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class RecipeOutcomes
{
    public const string Pending = "pending";
    public const string Declined = "declined";
    public const string Confirmed = "confirmed";

    public const int MaxLength = 16;
}
