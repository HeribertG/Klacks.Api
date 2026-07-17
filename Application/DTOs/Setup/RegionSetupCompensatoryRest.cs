// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupCompensatoryRest
{
    public bool? Enabled { get; set; }

    public int? DeadlineDays { get; set; }

    public bool? AutoPlan { get; set; }
}
