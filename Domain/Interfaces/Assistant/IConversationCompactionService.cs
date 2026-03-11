// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Komprimiert lange Konversationen durch LLM-basierte Zusammenfassungen statt sie abzuschneiden.
/// </summary>
/// <param name="conversationId">Eindeutige Konversations-ID (nicht die DB-GUID)</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConversationCompactionService
{
    Task CompactIfNeededAsync(string conversationId, CancellationToken cancellationToken = default);
}
