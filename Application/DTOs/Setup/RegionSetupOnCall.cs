// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupOnCall
{
    public bool? Enabled { get; set; }

    public int? PresenceCountsPercent { get; set; }

    public int? StandbyCountsPercent { get; set; }

    public bool? IncludeInPeriodCaps { get; set; }
}
