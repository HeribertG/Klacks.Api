// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupQualifications
{
    public bool? ExpiredMandatoryBlocks { get; set; }

    public int? ExpiryWarningDays { get; set; }
}
