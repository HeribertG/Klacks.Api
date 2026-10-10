// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The weekday tokens accepted and produced for the contract working-weekday list (comma separated,
/// case-insensitive when parsed), in calendar order starting on Monday.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContractWorkdayTokens
{
    public const string Monday = "Mon";

    public const string Tuesday = "Tue";

    public const string Wednesday = "Wed";

    public const string Thursday = "Thu";

    public const string Friday = "Fri";

    public const string Saturday = "Sat";

    public const string Sunday = "Sun";

    public const char Separator = ',';

    public static readonly IReadOnlyList<(string Token, DayOfWeek Day)> InCalendarOrder =
    [
        (Monday, DayOfWeek.Monday),
        (Tuesday, DayOfWeek.Tuesday),
        (Wednesday, DayOfWeek.Wednesday),
        (Thursday, DayOfWeek.Thursday),
        (Friday, DayOfWeek.Friday),
        (Saturday, DayOfWeek.Saturday),
        (Sunday, DayOfWeek.Sunday)
    ];
}
