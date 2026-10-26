// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a synchronous geocoding run did: the addresses it filled (with source), how many the geocoder could not
/// place, how many met an unreachable geocoder, how many had changed or got coordinates in the meantime
/// (superseded, left alone), how many were handed to the background backfill and how many were left over without
/// being handed on (deferred: the backfill does not run on this instance, so only another call reaches them).
/// </summary>

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingRunSummary(
    bool GeocodingDisabled,
    IReadOnlyList<AddressGeocodingFilledAddress> Filled,
    int NotFound,
    int Unavailable,
    int Superseded,
    int Queued,
    int Deferred = 0)
{
    public static AddressGeocodingRunSummary Disabled { get; } = new(true, [], 0, 0, 0, 0);
}
