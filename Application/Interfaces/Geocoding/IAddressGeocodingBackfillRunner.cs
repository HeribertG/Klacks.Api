// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One pass of the background address geocoding: reads one batch of due addresses and geocodes them one by one,
/// pausing after every real geocoder request. Returns true when a full batch went through without stopping (more
/// addresses are due right away); false when the batch was short or stopped at an unreachable or switched-off
/// geocoder, so the caller waits for the next scan interval.
/// </summary>

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IAddressGeocodingBackfillRunner
{
    Task<bool> RunBatchAsync(CancellationToken cancellationToken);
}
