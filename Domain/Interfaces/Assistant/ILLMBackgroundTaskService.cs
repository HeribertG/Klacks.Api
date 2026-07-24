// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Service for asynchronous background tasks after LLM interactions.
/// </summary>
public interface ILLMBackgroundTaskService
{
    void RunBackgroundTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> allFunctionCalls);

    /// <summary>
    /// Fire-and-forget compaction trigger for task-boundary events (e.g. AgentPlan completion) that
    /// need a different message-count threshold than the default post-turn compaction.
    /// </summary>
    /// <param name="conversationId">Unique conversation ID whose old messages may be compacted.</param>
    /// <param name="minMessages">Minimum message count required before compaction runs.</param>
    void TriggerConversationCompaction(string conversationId, int minMessages);
}
