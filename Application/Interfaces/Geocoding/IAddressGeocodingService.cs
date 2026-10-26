// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The system function that gives addresses coordinates on behalf of a trigger (Klacksy for a user, the import,
/// the admin batch). It only fills empty coordinates, applies the privacy rule for employee addresses and hands
/// what it does not do synchronously to the background backfill. Visibility is the caller's job: it passes only
/// addresses the user may see.
/// </summary>

using Klacks.Api.Application.DTOs.Geocoding;

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IAddressGeocodingService
{
    bool IsGeocodingEnabled { get; }

    /// <summary>
    /// Splits addresses without coordinates into eligible, privacy-blocked and incomplete ones.
    /// </summary>
    Task<AddressGeocodingSelection> SelectAsync(
        IReadOnlyList<AddressGeocodingCandidate> candidates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Geocodes eligible addresses in order within the budget and requests the remainder for the backfill.
    /// </summary>
    Task<AddressGeocodingRunSummary> GeocodeAsync(
        IReadOnlyList<AddressGeocodingCandidate> eligible,
        AddressGeocodingAttribution attribution,
        AddressGeocodingBudget budget,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the eligible ones among the given addresses for the background backfill; returns how many were
    /// requested (0 while geocoding or the backfill is switched off).
    /// </summary>
    Task<int> RequestAsync(
        IReadOnlyCollection<Guid> addressIds, AddressGeocodingAttribution attribution, CancellationToken cancellationToken = default);
}
