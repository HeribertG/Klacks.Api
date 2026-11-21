// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Contract of the UPDATER_CAPABILITIES report the out-of-process updater writes into the settings table:
/// {"updaterVersion","mode","capabilities":[...],"reportedAtUtc"}. The updater reports every 15 minutes (retry after one
/// minute) and pauses while an engine upgrade runs; a report older than the freshness threshold counts as no updater.
/// </summary>
public static class UpdaterCapabilityContract
{
    public const string EngineUpgradeCapability = "postgres-engine-upgrade/1";

    public const string CapabilitiesProperty = "capabilities";

    public const string ReportedAtProperty = "reportedAtUtc";

    public const int FreshnessThresholdMinutes = 20;

    public static readonly TimeSpan FreshnessThreshold = TimeSpan.FromMinutes(FreshnessThresholdMinutes);
}
