// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Settings.GeoServices;

public class MapTileServiceOptions
{
    public bool Enabled { get; set; } = true;

    public string? UrlTemplate { get; set; }

    public string? Subdomains { get; set; }

    public string? Attribution { get; set; }

    public int? MaxZoom { get; set; }
}
