// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the UPDATER_CAPABILITIES report and decides whether the running updater can execute a PostgreSQL engine upgrade.
/// Missing: no report, unreadable JSON, no timestamp or the capability is not listed. Stale: the timestamp lies more than
/// the freshness threshold away from now, in either direction (a far future timestamp is as untrustworthy as an old one).
/// Only a fresh report that lists the capability is Supported; nothing else ever allows the upgrade.
/// </summary>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Services.Update;

public static class UpdaterCapabilityEvaluator
{
    public static UpdaterCapabilityState EvaluateEngineUpgrade(string? report, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(report))
        {
            return UpdaterCapabilityState.Missing;
        }

        try
        {
            using var document = JsonDocument.Parse(report);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !ListsEngineUpgrade(root)
                || !TryReadReportedAt(root, out var reportedAtUtc))
            {
                return UpdaterCapabilityState.Missing;
            }

            return (nowUtc - reportedAtUtc).Duration() <= UpdaterCapabilityContract.FreshnessThreshold
                ? UpdaterCapabilityState.Supported
                : UpdaterCapabilityState.Stale;
        }
        catch (JsonException)
        {
            return UpdaterCapabilityState.Missing;
        }
    }

    private static bool ListsEngineUpgrade(JsonElement root)
    {
        return root.TryGetProperty(UpdaterCapabilityContract.CapabilitiesProperty, out var capabilities)
            && capabilities.ValueKind == JsonValueKind.Array
            && capabilities.EnumerateArray().Any(capability =>
                capability.ValueKind == JsonValueKind.String
                && string.Equals(capability.GetString(), UpdaterCapabilityContract.EngineUpgradeCapability, StringComparison.Ordinal));
    }

    private static bool TryReadReportedAt(JsonElement root, out DateTime reportedAtUtc)
    {
        reportedAtUtc = default;
        return root.TryGetProperty(UpdaterCapabilityContract.ReportedAtProperty, out var reportedAt)
            && reportedAt.ValueKind == JsonValueKind.String
            && DateTime.TryParse(reportedAt.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out reportedAtUtc);
    }
}
