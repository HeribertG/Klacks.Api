// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistence gateway for order-review draft rows. ExistsAsync tells whether an unexpired row exists without reading
/// its JSON. GetAsync returns the outstanding draft for a
/// user/conversation (or null); UpsertAsync stores exactly one row per (user, conversation), replacing
/// any prior draft; DeleteAsync hard-removes the row for a user/conversation; PruneExpiredAsync
/// hard-removes every row whose TTL elapsed.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingOrderReviewDraftRepository
{
    Task<PendingOrderReviewDraftRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, string conversationId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task UpsertAsync(PendingOrderReviewDraftRow row, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
