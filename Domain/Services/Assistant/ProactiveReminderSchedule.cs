// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure schedule arithmetic for proactive reminders (package F "repeat until acknowledged"). Takes no
/// dependency on the clock, the database or delivery: given "now" and how many reminders a dispatch row
/// has already received, it decides when the next reminder falls due. Steps beyond the schedule repeat
/// the last entry of ProactiveReminderDefaults.BackoffHours, so a row only
/// ever stops once the user acknowledges it.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ProactiveReminderSchedule
{
    /// <summary>
    /// When the first reminder is due for a dispatch row that was just delivered.
    /// </summary>
    /// <param name="nowUtc">Current instant.</param>
    public static DateTime FirstDueAfter(DateTime nowUtc)
    {
        return nowUtc.AddHours(ProactiveReminderDefaults.BackoffHours[0]);
    }

    /// <summary>
    /// When the next reminder is due after <paramref name="remindersSent"/> reminders already went out.
    /// Reminder counts beyond the schedule length keep repeating the last backoff interval.
    /// </summary>
    /// <param name="remindersSent">How many reminders the row has already received (0 = only the initial dispatch).</param>
    /// <param name="nowUtc">Current instant.</param>
    public static DateTime NextDueAfter(int remindersSent, DateTime nowUtc)
    {
        var step = Math.Clamp(remindersSent, 0, ProactiveReminderDefaults.BackoffHours.Length - 1);
        return nowUtc.AddHours(ProactiveReminderDefaults.BackoffHours[step]);
    }
}
