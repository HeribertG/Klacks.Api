// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits and texts of the Idempotency-Key handling on the creating endpoints: how long an answer is kept for a repeat,
/// how many answers are kept at most in total and per user, and the only accepted key format (a GUID with hyphens, as
/// crypto.randomUUID writes it).
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class IdempotentRequestConstants
{
    public const int ResponseLifetimeMinutes = 10;

    public const int MaxStoredResponses = 500;

    public const int MaxEntriesPerUser = 50;

    public const string KeyFormat = "D";

    public const string InvalidKeyMessage = "The Idempotency-Key header must be a single GUID (format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).";

    public static readonly TimeSpan ResponseLifetime = TimeSpan.FromMinutes(ResponseLifetimeMinutes);
}
