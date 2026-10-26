// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocodes one address to its locality centroid (postcode and city) through the persistent locality cache and
/// fills the coordinates only when they are still empty. The caller has applied the privacy rule already.
/// </summary>

using Klacks.Api.Application.DTOs.Geocoding;

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IAddressGeocodingFiller
{
    Task<AddressGeocodingFillResult> FillAsync(
        AddressGeocodingCandidate candidate, AddressGeocodingAttribution attribution, CancellationToken cancellationToken = default);
}
