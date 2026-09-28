// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Config;

public class MarketplaceRegionPackageInfo
{
    [JsonPropertyName("countryCode")]
    public string Country { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string MinKlacksVersion { get; set; } = string.Empty;
}
