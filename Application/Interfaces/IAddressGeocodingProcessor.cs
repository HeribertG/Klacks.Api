// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocodes one stored client address: on an exact hit it fills latitude/longitude (and the state when
/// it is empty); without a hit the address stays unchanged.
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IAddressGeocodingProcessor
{
    Task ProcessAsync(Guid addressId, CancellationToken cancellationToken);
}
