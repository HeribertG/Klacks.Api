// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// Marketplace package identity of the region setup profile. Both fields are required when the
/// block is present; the values are written to settings on EVERY setup run (never marker-gated)
/// so a newer package version replaces the recorded identity.
/// </summary>
/// <param name="Country">Two-letter ISO country code of the package</param>
/// <param name="Version">Non-empty package version string</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupPackage
{
    public string? Country { get; set; }

    public string? Version { get; set; }
}
