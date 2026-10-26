// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// On whose behalf a geocoding request or fill happens: the background scan, Klacksy for a user, the import or the
/// admin batch of a user, or the container autofill. Stored with the request and with the fill (audit).
/// </summary>
/// <param name="Trigger">What started the geocoding</param>
/// <param name="UserId">The user it ran for, or null for the system scan</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingAttribution(AddressGeocodingTrigger Trigger, Guid? UserId)
{
    public static AddressGeocodingAttribution SystemScan { get; } = new(AddressGeocodingTrigger.SystemScan, null);
}
