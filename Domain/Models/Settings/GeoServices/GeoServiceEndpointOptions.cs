// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Settings.GeoServices;

public class GeoServiceEndpointOptions
{
    public bool Enabled { get; set; } = true;

    public string? BaseUrl { get; set; }
}
