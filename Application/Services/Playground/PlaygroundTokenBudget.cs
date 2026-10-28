// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// In-memory daily token budget of the Playground with a global, a per-IP (IPv4 or IPv6 /64) and a per-device
/// bucket. The budget day rolls at the configured UTC boundary hour (02:00), not at midnight. Reserve and
/// settle run under one lock so the three buckets are booked atomically; a bucket is open while its used
/// tokens are below its limit, so the call that crosses the line is the last one let through. Entries of
/// earlier days are dropped on rollover. State lives in memory only and starts empty after a restart.
/// </summary>
/// <param name="timeProvider">Clock the budget day is derived from, injected so a test can drive it</param>
/// <param name="options">The three daily limits; non-positive values fall back to the defaults</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Playground;
using Klacks.Api.Domain.Services.Playground;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundTokenBudget : IPlaygroundTokenBudget
{
    private readonly object _gate = new();
    private readonly Dictionary<string, long> _ipUsed = new();
    private readonly Dictionary<string, long> _deviceUsed = new();
    private readonly TimeProvider _timeProvider;
    private readonly long _globalLimit;
    private readonly long _ipLimit;
    private readonly long _deviceLimit;
    private string _dayKey = string.Empty;
    private long _globalUsed;

    public PlaygroundTokenBudget(TimeProvider timeProvider, IOptions<PlaygroundTokenLimitOptions> options)
    {
        _timeProvider = timeProvider;
        var configured = options.Value;
        _globalLimit = PlaygroundTokenLimitOptions.Positive(configured.GlobalDailyTokens, PlaygroundTokenLimitConstants.DefaultGlobalDailyTokens);
        _ipLimit = PlaygroundTokenLimitOptions.Positive(configured.PerIpDailyTokens, PlaygroundTokenLimitConstants.DefaultPerIpDailyTokens);
        _deviceLimit = PlaygroundTokenLimitOptions.Positive(configured.PerDeviceDailyTokens, PlaygroundTokenLimitConstants.DefaultPerDeviceDailyTokens);
    }

    public bool IsExhausted(PlaygroundVisitor? visitor)
    {
        lock (_gate)
        {
            RollOverIfNewDay();
            return !HasRoom(visitor?.IpBucketKey, visitor?.DeviceId);
        }
    }

    public PlaygroundBudgetReservation? TryReserve(PlaygroundVisitor? visitor, long estimatedTokens)
    {
        var tokens = Math.Max(estimatedTokens, 0);
        var ipKey = visitor?.IpBucketKey;
        var deviceKey = visitor?.DeviceId;

        lock (_gate)
        {
            RollOverIfNewDay();
            if (!HasRoom(ipKey, deviceKey))
            {
                return null;
            }

            Book(ipKey, deviceKey, tokens);
            return new PlaygroundBudgetReservation(_dayKey, ipKey, deviceKey, tokens);
        }
    }

    public void Settle(PlaygroundBudgetReservation reservation, long actualTokens)
    {
        var actual = Math.Max(actualTokens, 0);

        lock (_gate)
        {
            RollOverIfNewDay();
            if (!string.Equals(reservation.DayKey, _dayKey, StringComparison.Ordinal))
            {
                return;
            }

            Book(reservation.IpKey, reservation.DeviceKey, actual - reservation.ChargedTokens);
            reservation.ChargedTokens = actual;
        }
    }

    public void Release(PlaygroundBudgetReservation reservation) => Settle(reservation, 0);

    private bool HasRoom(string? ipKey, string? deviceKey) =>
        _globalUsed < _globalLimit
        && (ipKey is null || UsedOf(_ipUsed, ipKey) < _ipLimit)
        && (deviceKey is null || UsedOf(_deviceUsed, deviceKey) < _deviceLimit);

    private void Book(string? ipKey, string? deviceKey, long delta)
    {
        _globalUsed = Math.Max(_globalUsed + delta, 0);
        if (ipKey is not null)
        {
            _ipUsed[ipKey] = Math.Max(UsedOf(_ipUsed, ipKey) + delta, 0);
        }

        if (deviceKey is not null)
        {
            _deviceUsed[deviceKey] = Math.Max(UsedOf(_deviceUsed, deviceKey) + delta, 0);
        }
    }

    private static long UsedOf(Dictionary<string, long> bucket, string key) =>
        bucket.TryGetValue(key, out var used) ? used : 0;

    private void RollOverIfNewDay()
    {
        var today = CurrentDayKey();
        if (string.Equals(today, _dayKey, StringComparison.Ordinal))
        {
            return;
        }

        _dayKey = today;
        _globalUsed = 0;
        _ipUsed.Clear();
        _deviceUsed.Clear();
    }

    private string CurrentDayKey() => _timeProvider
        .GetUtcNow()
        .UtcDateTime
        .AddHours(-PlaygroundTokenLimitConstants.DayBoundaryHourUtc)
        .ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
