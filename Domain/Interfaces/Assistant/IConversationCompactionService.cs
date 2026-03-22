// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compresses long conversations using LLM-based summaries instead of truncating them.
/// </summary>
/// <param name="conversationId">Unique conversation ID (not the DB GUID)</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConversationCompactionService
{
    Task CompactIfNeededAsync(string conversationId, CancellationToken cancellationToken = default);
}
