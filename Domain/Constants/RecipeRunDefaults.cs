// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Policy values of the recipe_runs funnel telemetry (W1.5).
/// </summary>
public static class RecipeRunDefaults
{
    /// <summary>
    /// How long a Running row may go untouched before the sweep counts it as Expired. Deliberately
    /// far longer than RecipeEngineDefaults.PendingRecipeTtlMinutes: the pending store drops the
    /// plan after 30 minutes, but a user who comes back the same day still reads as one attempt, and
    /// a run left Running forever would silently inflate the funnel's denominator.
    /// </summary>
    public static readonly TimeSpan ExpireAfter = TimeSpan.FromHours(24);

    /// <summary>Storage cap for RecipeRun.AbortReason.</summary>
    public const int AbortReasonMaxLength = 500;
}
