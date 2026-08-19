// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared day-length and end-of-day values. Before these existed the codebase carried four
/// competing end-of-day markers (23:59:00, 23:59:59, 00:00:00, TimeOnly.MaxValue) and seven
/// private copies of HoursPerDay/MinutesPerDay, which is how two mutually contradictory
/// duration conventions could both live in the system and both pass their tests.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class DayTimeConstants
{
    public const int HoursPerDay = 24;

    public const int MinutesPerHour = 60;

    public const int MinutesPerDay = HoursPerDay * MinutesPerHour;

    /// <summary>
    /// Midnight, the start of a day. Also the value TimeOnlyJsonConverter substitutes for an
    /// omitted time field, so an omitted pair is indistinguishable from a deliberate 00:00-00:00.
    /// </summary>
    public static readonly TimeOnly Midnight = new(0, 0);

    /// <summary>
    /// The booking marker for a full day, used as the end of an all-day absence or break.
    /// Full-day absences are deliberately booked as Midnight..LastMinuteOfDay and never as
    /// Midnight..Midnight, so the equality case stays free for working time.
    /// </summary>
    public static readonly TimeOnly LastMinuteOfDay = new(23, 59);
}
