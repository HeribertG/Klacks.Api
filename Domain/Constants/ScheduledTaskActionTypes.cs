// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// The kinds of action a scheduled task can perform when it fires.
/// </summary>
public static class ScheduledTaskActionTypes
{
    /// <summary>Deliver a static text message to the owner.</summary>
    public const string Reminder = "reminder";

    /// <summary>Run a single deterministic skill on behalf of the owner and deliver its result.</summary>
    public const string Skill = "skill";
}
