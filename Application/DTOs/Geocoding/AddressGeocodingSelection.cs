// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Addresses without coordinates split by the geocoding rules: eligible ones, private addresses of employees the
/// installation does not allow to geocode, and incomplete ones without postcode or city.
/// </summary>

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingSelection(
    IReadOnlyList<AddressGeocodingCandidate> Eligible,
    IReadOnlyList<AddressGeocodingCandidate> PrivacyBlocked,
    IReadOnlyList<AddressGeocodingCandidate> Incomplete);
