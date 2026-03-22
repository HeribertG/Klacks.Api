// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for automatic extraction and storage of important information from chat turns as memories.
/// </summary>
/// <param name="agentId">ID of the agent to whom the memories are assigned</param>
/// <param name="userMessage">User message in the completed turn</param>
/// <param name="assistantResponse">Assistant response in the completed turn</param>
/// <param name="userId">ID of the authenticated user as string</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAutoMemoryExtractionService
{
    Task ExtractAndStoreMemoriesAsync(Guid agentId, string userMessage, string assistantResponse, string userId);
}
