// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads geocoding candidates and writes coordinates, attempts and requests of addresses. Self-committing: every
/// write is its own atomic statement or transaction (the callers are the background backfill, Klacksy skills and
/// post-commit steps, none of which stage other changes in the same context). The coordinate write never
/// overwrites: it lands only on a live address that still has no coordinates and the geocoded postcode, city and
/// country, and it writes no user stamp (system write; the audit lives in the geocoding state).
/// </summary>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IAddressGeocodingStateRepository
{
    Task<List<AddressGeocodingCandidate>> GetDueCandidatesAsync(AddressGeocodingDueQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Live addresses of live clients whose latitude or longitude is missing, including those without postcode
    /// or city. With clientIds null every client is read, otherwise only the given ones.
    /// </summary>
    Task<List<AddressGeocodingCandidate>> GetMissingCoordinatesAsync(
        IReadOnlyCollection<Guid>? clientIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The addresses among the given ids that are live and still have no coordinates.
    /// </summary>
    Task<List<AddressGeocodingCandidate>> GetMissingCoordinatesByIdsAsync(
        IReadOnlyCollection<Guid> addressIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the coordinates (and the state, when empty) and records the fill; false when the address changed,
    /// got coordinates or was deleted in the meantime (nothing written then).
    /// </summary>
    Task<bool> TryFillAsync(AddressGeocodingFill fill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a geocoding answer that filled nothing (not found, unavailable) for the given locality fields and
    /// clears a pending request.
    /// </summary>
    Task RecordAttemptAsync(
        AddressGeocodingCandidate candidate, AddressGeocodingOutcome outcome, DateTime at, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the addresses as requested for the background backfill; returns how many were marked.
    /// </summary>
    Task<int> RequestAsync(
        IReadOnlyCollection<Guid> addressIds, AddressGeocodingAttribution attribution, DateTime at, CancellationToken cancellationToken = default);
}
