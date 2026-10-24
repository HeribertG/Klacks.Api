// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pace and windows of the address geocoding backfill. Nominatim allows one request per second for the whole
/// installation (GeocodingService already waits 1.1 s per request); the backfill adds a pause after each real
/// request so interactive address checks and searches still get the shared slot. A not-found locality is asked
/// again only after the not-found window, an unreachable geocoder after the short unavailable window.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AddressGeocodingLimits
{
    public const int ScanBatchSize = 50;

    public const int ScanIntervalMinutes = 10;

    public const int StartupDelaySeconds = 120;

    public const int PauseAfterGeocoderCallMilliseconds = 1000;

    public const int NotFoundRetryDays = 30;

    public const int UnavailableRetryMinutes = 60;

    public const int AssistantGeocoderCallBudget = 3;

    public const int AssistantFillLimit = 20;

    public const int AssistantReportedAddressLimit = 20;
}
