// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Daily token budgets of the Playground instance, bound to the configuration section
/// <c>Playground:TokenLimit</c> (environment: <c>Playground__TokenLimit__GlobalDailyTokens</c> etc.).
/// Fail-closed: a missing, empty, unparsable or non-positive value keeps the default.
/// </summary>
/// <param name="GlobalDailyTokens">Tokens all visitors together may spend per day</param>
/// <param name="PerIpDailyTokens">Tokens one IPv4 address or IPv6 /64 may spend per day</param>
/// <param name="PerDeviceDailyTokens">Tokens one device token may spend per day</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Playground;

public class PlaygroundTokenLimitOptions
{
    public const string SectionName = "Playground:TokenLimit";

    public int GlobalDailyTokens { get; set; } = PlaygroundTokenLimitConstants.DefaultGlobalDailyTokens;

    public int PerIpDailyTokens { get; set; } = PlaygroundTokenLimitConstants.DefaultPerIpDailyTokens;

    public int PerDeviceDailyTokens { get; set; } = PlaygroundTokenLimitConstants.DefaultPerDeviceDailyTokens;

    /// <summary>
    /// Reads the three limits from the section without ever throwing on bad input.
    /// </summary>
    /// <param name="section">The Playground:TokenLimit configuration section</param>
    public void ApplyConfiguration(IConfiguration section)
    {
        GlobalDailyTokens = ReadPositive(section[nameof(GlobalDailyTokens)], PlaygroundTokenLimitConstants.DefaultGlobalDailyTokens);
        PerIpDailyTokens = ReadPositive(section[nameof(PerIpDailyTokens)], PlaygroundTokenLimitConstants.DefaultPerIpDailyTokens);
        PerDeviceDailyTokens = ReadPositive(section[nameof(PerDeviceDailyTokens)], PlaygroundTokenLimitConstants.DefaultPerDeviceDailyTokens);
    }

    /// <param name="value">The configured value, null or empty when unset</param>
    /// <param name="fallback">Default used when the value is absent or not a positive integer</param>
    public static int ReadPositive(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : fallback;

    /// <param name="value">A limit as held by the options, possibly overwritten programmatically</param>
    /// <param name="fallback">Default used when the value is not positive</param>
    public static int Positive(int value, int fallback) => value > 0 ? value : fallback;
}
