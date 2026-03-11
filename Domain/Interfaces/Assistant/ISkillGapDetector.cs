// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects recurring unfulfilled user requests and records them as skill gaps for later review.
/// </summary>
/// <param name="agentId">ID of the agent handling the conversation</param>
/// <param name="userMessage">The user's message in the completed turn</param>
/// <param name="assistantResponse">The assistant's response in the completed turn</param>
/// <param name="hadFunctionCalls">True if the LLM executed any function/skill during the turn</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillGapDetector
{
    Task DetectAndSuggestAsync(Guid agentId, string userMessage, string assistantResponse, bool hadFunctionCalls);
}
