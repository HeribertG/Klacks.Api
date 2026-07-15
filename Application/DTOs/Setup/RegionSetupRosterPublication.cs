// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupRosterPublication
{
    public int? MinLeadDays { get; set; }

    public bool? CountWorkdaysOnly { get; set; }
}
