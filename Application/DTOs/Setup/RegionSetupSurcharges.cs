// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupSurcharges
{
    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? We1Rate { get; set; }

    public decimal? We2Rate { get; set; }

    public decimal? We3Rate { get; set; }
}
