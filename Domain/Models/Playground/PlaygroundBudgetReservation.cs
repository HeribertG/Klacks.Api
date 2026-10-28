// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handle of one atomic reservation against the Playground token buckets. It remembers the day it was
/// made on and which buckets it was booked into, so settling it later adjusts exactly those buckets and
/// never a different day's.
/// </summary>
/// <param name="dayKey">Budget day (rolls at the day boundary hour) the reservation was booked on</param>
/// <param name="ipKey">IP bucket key, null when the visitor had no known address</param>
/// <param name="deviceKey">Device bucket key, null when the visitor had no valid device token</param>
/// <param name="chargedTokens">Tokens currently booked into each bucket; adjusted when the real usage is known</param>

namespace Klacks.Api.Domain.Models.Playground;

public sealed class PlaygroundBudgetReservation
{
    public PlaygroundBudgetReservation(string dayKey, string? ipKey, string? deviceKey, long chargedTokens)
    {
        DayKey = dayKey;
        IpKey = ipKey;
        DeviceKey = deviceKey;
        ChargedTokens = chargedTokens;
    }

    public string DayKey { get; }

    public string? IpKey { get; }

    public string? DeviceKey { get; }

    public long ChargedTokens { get; internal set; }
}
