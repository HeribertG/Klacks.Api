// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compresses long conversations using LLM-based summaries instead of truncating them.
/// </summary>
/// <param name="conversationId">Unique conversation ID (not the DB GUID)</param>
/// <param name="userId">Owner of the conversation; a conversation belonging to anyone else is never compacted.</param>
/// <param name="minMessages">Minimum message count required before compaction runs; the parameterless overload uses the service's default threshold.</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConversationCompactionService
{
    Task CompactIfNeededAsync(string conversationId, string userId, CancellationToken cancellationToken = default);

    Task CompactIfNeededAsync(string conversationId, string userId, int minMessages, CancellationToken cancellationToken = default);
}
