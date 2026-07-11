// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupExport
{
    public List<string>? EnabledFormats { get; set; }

    public string? DefaultPayrollTargetSystem { get; set; }
}
