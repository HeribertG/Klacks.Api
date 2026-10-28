// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Names, codes and fail-closed defaults of the daily token limit that applies only to the public
/// Playground instance (Deployment:IsPlayground). The defaults also stand in for every missing, empty
/// or non-positive configuration value, so a misconfiguration can never mean "unlimited".
/// </summary>
public static class PlaygroundTokenLimitConstants
{
    public const string DeviceHeaderName = "X-Playground-Device";

    public const string ErrorCode = "PLAYGROUND_TOKEN_LIMIT";

    public const string LimitReachedMessage = "The daily token budget of the Playground is used up. Please try again tomorrow.";

    public const string DeviceTokenPurpose = "Klacks.Playground.DeviceToken.v1";

    public const string StatusRoute = "api/backend/Playground";

    public const string StatusAction = "Status";

    public const int DefaultGlobalDailyTokens = 1_500_000;

    public const int DefaultPerIpDailyTokens = 300_000;

    public const int DefaultPerDeviceDailyTokens = 150_000;

    /// <summary>
    /// Ceiling of the estimate one provider call may hold in the buckets while it is in flight. A normal turn reserves
    /// about 15,000 tokens; 64,000 is roughly four of them and well above the fixed parts of a worst-case turn (largest
    /// toolset reserve with its 30 percent margin, 8,000 interaction headroom, 2,000 safety margin, 2,000 requested output).
    /// It is a judgement value, not a measurement of the largest legitimate turn. The ceiling only bounds the up-front
    /// estimate: the usage the provider reports afterwards replaces it, so a larger real call is still booked in full,
    /// but one oversized request can no longer empty the global budget before it has run.
    /// </summary>
    public const int MaxReservationTokens = 64_000;

    public const int DayBoundaryHourUtc = 2;

    public const int Ipv6PrefixBytes = 8;

    public const int Ipv6PrefixBits = 64;

    public const int MaxDeviceTokenLength = 512;

    public const string DeviceIdFormat = "N";

    /// <summary>
    /// Share of a cache-read prompt token that counts against the budget. Cache reads are billed at a
    /// fraction by the provider, but they still prove the visitor sent a large prompt, and the provider
    /// usage reports them separately from the uncached input. 100 keeps them at full weight (the same
    /// definition as LLMUsage.TotalTokens), the conservative choice for a cost cap.
    /// </summary>
    public const int CacheReadTokenWeightPercent = 100;

    public const int PercentBase = 100;
}
