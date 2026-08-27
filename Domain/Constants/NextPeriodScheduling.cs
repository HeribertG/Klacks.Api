// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timing of the next-period scheduling readiness check. Defined here, like ProactiveHeartbeat,
/// so the lead window is a named installation constant rather than a magic number inside the
/// detector that watches it.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class NextPeriodScheduling
{
    /// <summary>Days before the next pay-period's start by which a schedule draft should exist.</summary>
    public const int LeadTimeDays = 7;
}
