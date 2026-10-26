// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocoding bookkeeping of one address, kept beside the address instead of on it: the client and address
/// saves copy every address column from the incoming resource (SetValues / Put), which would wipe the attempt
/// marker and the audit on each save. The attempt is pinned to the postcode, city and country it was made for,
/// so an edited address counts as not attempted again. A pending request (import, Klacksy, admin batch) moves
/// the address to the front of the background scan; the fill columns record which source wrote the stored
/// coordinates, when, on whose behalf and with which values (coordinates that differ from them were entered by
/// hand or by the address validation on save).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Geocoding;

public class AddressGeocodingState
{
    public Guid AddressId { get; set; }

    public DateTime? AttemptedAt { get; set; }

    public string? AttemptedZip { get; set; }

    public string? AttemptedCity { get; set; }

    public string? AttemptedCountry { get; set; }

    public AddressGeocodingOutcome? Outcome { get; set; }

    public DateTime? RequestedAt { get; set; }

    public AddressGeocodingTrigger? RequestedBy { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public DateTime? FilledAt { get; set; }

    public AddressGeocodingSource? FillSource { get; set; }

    public AddressGeocodingTrigger? FilledBy { get; set; }

    public Guid? FilledByUserId { get; set; }

    public double? FilledLatitude { get; set; }

    public double? FilledLongitude { get; set; }
}
