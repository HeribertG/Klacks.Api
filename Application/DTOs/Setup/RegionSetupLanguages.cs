// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// Language block of the region setup profile.
/// </summary>
/// <param name="Install">Language plugin codes to install on first boot</param>
/// <param name="Default">Default UI language of the installation; must be a core language, an installed plugin or listed in Install</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupLanguages
{
    public List<string>? Install { get; set; }

    public string? Default { get; set; }
}
