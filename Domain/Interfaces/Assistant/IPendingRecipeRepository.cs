// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence gateway for paused-recipe rows. Get returns the outstanding row for a user/conversation
/// (or null); Upsert stores exactly one row per (user, conversation), replacing any prior pause; Delete
/// hard-removes the row for a user/conversation; PruneExpired hard-removes every row whose TTL elapsed.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingRecipeRepository
{
    Task<PendingRecipeRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task UpsertAsync(PendingRecipeRow row, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
