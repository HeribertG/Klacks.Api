// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One entry of the top-level macros section (K20 entity import): ships or replaces a calculation
/// macro per country profile. Name and Content are required; Content is compiled at import time and a
/// script error fails the setup fast. Function (custom|standard|standardAdditive, default custom) and
/// Category (shift|vacation|illness|accident|workshopPaid|workshopUnpaid, default shift) place the
/// macro; importing a standard function demotes the current SEEDED (or unedited imported) holder to
/// custom — a customer-created holder is never displaced and fails the import instead.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupMacro
{
    public string? Name { get; set; }

    public string? Content { get; set; }

    public string? Function { get; set; }

    public string? Category { get; set; }
}
