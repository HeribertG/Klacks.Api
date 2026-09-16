// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence gateway for the previous-action record. Get returns the row of a (user, conversation)
/// pair or null; Upsert keeps exactly one row per pair, replacing any earlier one; PruneExpired hard-
/// removes every row whose TTL elapsed. Self-committing (Assistant convention): every method that
/// writes calls SaveChangesAsync itself, because the callers are the chat pipeline's own store, not an
/// HTTP handler with a unit of work.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantLastActionRepository
{
    Task<AssistantLastActionRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task UpsertAsync(AssistantLastActionRow row, CancellationToken cancellationToken = default);

    Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
