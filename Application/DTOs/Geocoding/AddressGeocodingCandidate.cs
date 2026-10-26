// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A live address without coordinates, with what the geocoding needs to decide and to ask: the owning client's
/// type (privacy rule) and display name (Klacksy's report), the locality fields exactly as stored (they pin the
/// conditional write), the current state (filled only when empty) and, for the background scan, the pending
/// request whose trigger and user the fill is attributed to.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingCandidate(
    Guid AddressId,
    Guid ClientId,
    EntityTypeEnum ClientType,
    string ClientName,
    string Zip,
    string City,
    string Country,
    string State,
    AddressGeocodingAttribution? PendingRequest = null);
