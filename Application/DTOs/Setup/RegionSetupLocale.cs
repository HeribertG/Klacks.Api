// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupLocale
{
    public string? Country { get; set; }

    public string? State { get; set; }

    public string? TimeZone { get; set; }

    public RegionSetupCalendarSelection? CalendarSelection { get; set; }
}
