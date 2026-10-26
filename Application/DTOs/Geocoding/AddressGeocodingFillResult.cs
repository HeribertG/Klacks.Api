// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of geocoding one address. UsedGeocoder tells whether a real Nominatim request was made (the backfill
/// pauses only after those, cache hits are free).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingFillResult(
    AddressGeocodingFillStatus Status,
    bool UsedGeocoder,
    double? Latitude = null,
    double? Longitude = null,
    AddressGeocodingSource? Source = null);
