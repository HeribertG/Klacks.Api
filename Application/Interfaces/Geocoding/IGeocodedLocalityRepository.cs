// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistent per-locality geocoding cache. Self-committing; StoreAsync is an upsert, so two writers resolving the
/// same locality at once (Klacksy and the backfill) never fail and the newer answer wins.
/// </summary>

using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Domain.Services.Geocoding;

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IGeocodedLocalityRepository
{
    Task<GeocodedLocality?> GetAsync(GeocodingLocalityKey key, CancellationToken cancellationToken = default);

    Task StoreAsync(GeocodedLocality locality, CancellationToken cancellationToken = default);
}
