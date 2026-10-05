// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The name a skill may show for a resolved holiday calendar: the calendar selection's name, or the company
/// country/region pair in token notation (e.g. "CH-BE") when the old country/region fallback applies; null when no
/// calendar applies. Never an id.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class HolidayCalendarDisplayName
{
    public static string? Of(ResolvedHolidayCalendarSource calendar) => calendar.Source switch
    {
        HolidayCalendarSource.CompanyCountryState => CalendarTokenParser.Format(calendar.Country ?? string.Empty, calendar.State ?? string.Empty),
        HolidayCalendarSource.None => null,
        _ => calendar.CalendarName,
    };
}
