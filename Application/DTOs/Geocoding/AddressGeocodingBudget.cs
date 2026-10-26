// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How much of a geocoding run happens synchronously. Klacksy may make only a few real geocoder requests inside
/// a chat turn; the admin batch has no limit. What does not fit (and every address whose geocoder was unreachable)
/// is handed to the background backfill when QueueRemainder is set.
/// </summary>
/// <param name="MaxGeocoderCalls">Most real Nominatim requests, or null for no limit</param>
/// <param name="MaxFills">Most addresses processed synchronously, or null for no limit</param>
/// <param name="QueueRemainder">Request the rest for the background backfill</param>

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingBudget(int? MaxGeocoderCalls, int? MaxFills, bool QueueRemainder)
{
    public static AddressGeocodingBudget Unlimited { get; } = new(null, null, true);
}
