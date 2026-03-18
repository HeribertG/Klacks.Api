// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves relevant agent memories (pinned + hybrid-searched) for the current user message.
/// </summary>
/// <param name="agentId">The agent whose memories to search</param>
/// <param name="userMessage">Current user message used for embedding/keyword search</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IMemoryRetrievalService
{
    Task<string> RetrieveRelevantMemoriesAsync(Guid agentId, string userMessage, CancellationToken cancellationToken = default);
}
