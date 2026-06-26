// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Outcome markers stored on a scheduled task after a tick processed it.
/// </summary>
public static class ScheduledTaskRunStatus
{
    public const string Ok = "ok";

    public const string Error = "error";

    /// <summary>The occurrence was missed (e.g. server offline) and intentionally not run.</summary>
    public const string Skipped = "skipped";
}
