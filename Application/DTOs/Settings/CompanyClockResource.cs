// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Company time-zone and "today" payload for the UI, readable by any authenticated user (unlike
/// GeneralSettingsController, which is Admin-only). TimeZone is the IANA id (e.g. "Asia/Kolkata");
/// Today is the company's current calendar date (yyyy-MM-dd, per the DateOnly wire format); Source is
/// the resolution-chain step that produced TimeZone, serialized as CompanyTimeZoneSource.ToString()
/// ("Setting" | "AddressCountry" | "CalendarCountry" | "Utc") so the frontend can compare it directly
/// without an int-to-name mapping.
/// </summary>

namespace Klacks.Api.Application.DTOs.Settings;

public class CompanyClockResource
{
    public string TimeZone { get; set; } = string.Empty;

    public DateOnly Today { get; set; }

    public string Source { get; set; } = string.Empty;
}
