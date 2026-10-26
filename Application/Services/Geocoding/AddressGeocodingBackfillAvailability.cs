// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Whether this instance runs the background address geocoding (BackgroundServices:AddressGeocoding on and
/// geocoding enabled by the GeoServices configuration). Requests are only written when something drains them, so
/// "queued" counts reported to users and Klacksy stay truthful.
/// </summary>
/// <param name="isActive">True when the backfill worker is registered on this instance</param>

namespace Klacks.Api.Application.Services.Geocoding;

public sealed class AddressGeocodingBackfillAvailability
{
    public AddressGeocodingBackfillAvailability(bool isActive)
    {
        IsActive = isActive;
    }

    public bool IsActive { get; }
}
