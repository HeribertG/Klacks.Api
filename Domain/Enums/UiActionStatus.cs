// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Lifecycle of a UiAction skill execution (W1.4). The backend can only observe the dispatch; the
/// browser reports the real outcome back via the report endpoint, which moves the usage row from
/// Dispatched to Completed or Failed. This is what turns "booked as success before the browser ran
/// anything" into a truthful signal.
/// </summary>
public enum UiActionStatus
{
    /// <summary>Steps were handed to the client; the browser outcome is not yet known.</summary>
    Dispatched = 1,

    /// <summary>The frontend reported the action as completed successfully.</summary>
    Completed = 2,

    /// <summary>The frontend reported the action as failed.</summary>
    Failed = 3
}
