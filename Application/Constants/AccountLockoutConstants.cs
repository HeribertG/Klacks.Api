// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defines the ASP.NET Identity account-lockout thresholds used to throttle
/// brute-force password guessing on the local authentication path.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class AccountLockoutConstants
{
    public const int MaxFailedAccessAttempts = 5;

    public const int LockoutDurationMinutes = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(LockoutDurationMinutes);
}
