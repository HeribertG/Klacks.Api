// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defaults for the proactive reminder backoff (package F "repeat until acknowledged"): how long a
/// delivered dispatch row waits before it is reminded again while the user stays silent, and how the
/// reminder sweep bounds its own work per run. Acknowledgement is the only stop condition - a row the
/// user never reacted to keeps being reminded, with the last entry of BackoffHours repeating forever.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ProactiveReminderDefaults
{
    /// <summary>Hours to wait before the 1st, 2nd, 3rd and every later reminder.</summary>
    public static readonly int[] BackoffHours = [1, 4, 24, 48];

    /// <summary>Cap on reminders a single user receives within one sweep run.</summary>
    public const int MaxRemindersPerUserPerSweep = 10;

    /// <summary>Dispatch rows a sweep processes per batch before it yields.</summary>
    public const int SweepBatchSize = 200;
}
