// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupEnforcement
{
    public string? DefaultMode { get; set; }

    public RegionSetupEnforcementRules? Rules { get; set; }

    public bool? AllowSupervisorOverride { get; set; }
}
