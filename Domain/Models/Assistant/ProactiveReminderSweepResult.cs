// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one run of the reminder sweep (package F "repeat until acknowledged") did, per outcome rather
/// than as a single number. Every way a due dispatch row can be passed over has its own counter, so a
/// sweep that reports only its sends cannot hide rows that were stopped, deferred or lost to a
/// concurrent acknowledge.
/// </summary>
/// <param name="Due">Dispatch rows whose reminder was due when the sweep started.</param>
/// <param name="Reminded">Rows the sweep advanced and (for connected users) delivered again.</param>
/// <param name="Stopped">Rows taken out of the reminder loop because their condition is gone or terminal.</param>
/// <param name="Skipped">Rows deferred without a send (mute / snooze / minimum severity / per-user cap).</param>
/// <param name="Lost">Rows whose compare-and-swap lost - the user acknowledged or another instance won.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ProactiveReminderSweepResult(
    int Due,
    int Reminded,
    int Stopped,
    int Skipped,
    int Lost);
