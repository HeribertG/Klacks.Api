// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Retry limits of the OpenRouteService matrix request: how many attempts a transient failure (5xx, 429, network
/// error, timeout) gets, the pause that grows with each attempt, the upper bound for a server-sent Retry-After, and
/// the time one attempt may take before it counts as a timeout.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class OpenRouteServiceRetryConstants
{
    public const int MaxAttempts = 3;

    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    public static readonly TimeSpan MaxRetryAfterDelay = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromSeconds(30);
}
