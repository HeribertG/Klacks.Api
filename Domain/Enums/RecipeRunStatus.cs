// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Lifecycle of a recipe run (W1.5). A run starts when a forcing plan engages, stays Running while it
/// waits on an ask/confirmation step, and ends Completed (all steps done), Aborted (gate hold, user
/// cancellation, ambiguous customer or turn ended before completion) or Expired (pending store TTL
/// lapsed without the user returning).
/// </summary>
public enum RecipeRunStatus
{
    Running = 1,
    Completed = 2,
    Aborted = 3,
    Expired = 4
}
