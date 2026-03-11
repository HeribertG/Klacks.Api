// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service zur automatischen Extraktion und Speicherung wichtiger Informationen aus Chat-Turns als Memories.
/// </summary>
/// <param name="agentId">ID des Agents, dem die Memories zugeordnet werden</param>
/// <param name="userMessage">Nachricht des Users im abgeschlossenen Turn</param>
/// <param name="assistantResponse">Antwort des Assistants im abgeschlossenen Turn</param>
/// <param name="userId">ID des authentifizierten Users als string</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAutoMemoryExtractionService
{
    Task ExtractAndStoreMemoriesAsync(Guid agentId, string userMessage, string assistantResponse, string userId);
}
