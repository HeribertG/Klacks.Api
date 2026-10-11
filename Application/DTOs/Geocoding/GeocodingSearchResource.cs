// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public class GeocodingSearchResource
{
    [JsonConverter(typeof(JsonStringEnumConverter<GeocodingSearchOutcome>))]
    public GeocodingSearchOutcome Status { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? DisplayName { get; set; }
}
