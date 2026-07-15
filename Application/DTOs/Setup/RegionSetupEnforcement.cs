// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupEnforcement
{
    public string? DefaultMode { get; set; }

    public RegionSetupEnforcementRules? Rules { get; set; }

    public bool? AllowSupervisorOverride { get; set; }
}
