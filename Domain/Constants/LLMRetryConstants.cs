// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retry policy for transient LLM provider failures (rate limits, overload, gateway timeouts).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class LLMRetryConstants
{
    public const int MaxTransientRetries = 2;

    public const int BaseRetryDelayMs = 1000;

    /// <summary>
    /// Linear backoff: attempt 1 waits BaseRetryDelayMs, attempt 2 waits twice that.
    /// </summary>
    /// <param name="attempt">1-based retry attempt number</param>
    public static TimeSpan GetRetryDelay(int attempt) => TimeSpan.FromMilliseconds(BaseRetryDelayMs * attempt);
}
