// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limit of the chat command that deletes many Works with one sentence. MaxWorksPerCall is far below the REST bulk
/// cap (RateLimitingPolicies.MaxBulkOperationItems): a spoken request must never remove hundreds of entries at once,
/// and a call that matches more is refused as a whole instead of being cut off at the limit.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class BulkWorkDeletionDefaults
{
    public const int MaxWorksPerCall = 50;
}
