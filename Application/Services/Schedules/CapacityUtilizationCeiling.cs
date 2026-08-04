// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Interprets the configured utilization ceiling, the share of capacity that may be consumed by
/// scheduled shifts before an absence request is treated as a resource gap. The remainder is the
/// reserve kept for unplanned sickness. Accepts both a ratio (0.8) and a percentage (80) because the
/// setting has no UI yet and is entered by hand. Shared by the chat skills and the email pipeline so
/// one configured value governs every path.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Application.Services.Schedules;

public static class CapacityUtilizationCeiling
{
    public const double Default = 0.8;

    private const double PercentFactor = 100.0;

    public static double Parse(string? rawSettingValue)
    {
        if (string.IsNullOrWhiteSpace(rawSettingValue)
            || !double.TryParse(rawSettingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return Default;
        }

        if (parsed > 1 && parsed <= PercentFactor)
        {
            parsed /= PercentFactor;
        }

        return parsed is > 0 and <= 1 ? parsed : Default;
    }

    public static double ToPercent(double ratio) => Math.Round(ratio * PercentFactor, 1);
}
