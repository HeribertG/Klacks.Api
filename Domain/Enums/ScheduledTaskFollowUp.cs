// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What has to happen to a scheduled task after a run attempt. Pause keeps the owner's IsEnabled
/// intent and the schedule intact so lifting the pause is a pure toggle; Disable is terminal and
/// requires the task to be created again.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum ScheduledTaskFollowUp
{
    None = 0,
    Disable = 1,
    Pause = 2
}
