// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Geocoding;

/// <summary>
/// Outcome of a batch geocoding run: how many addresses lacked coordinates, how many got some and how many
/// did not, or that the geocoder is switched off and nothing was attempted.
/// </summary>
public sealed record GeocodeAllAddressesResult(bool GeocodingDisabled, int Total, int Geocoded, int Failed)
{
    public static GeocodeAllAddressesResult Disabled { get; } = new(true, 0, 0, 0);
}
