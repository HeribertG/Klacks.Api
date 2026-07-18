// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Config;

public class MarketplaceRegionPackageInfo
{
    [JsonPropertyName("countryCode")]
    public string Country { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string MinKlacksVersion { get; set; } = string.Empty;
}
