// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Settings.GeoServices;

public class MapTileConfigResource
{
    public bool Enabled { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<GeoServiceDisabledReason>))]
    public GeoServiceDisabledReason Reason { get; set; }

    public string? UrlTemplate { get; set; }

    public List<string> Subdomains { get; set; } = [];

    public string Attribution { get; set; } = string.Empty;

    public int MaxZoom { get; set; }
}
