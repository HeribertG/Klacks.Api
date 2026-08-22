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
    /// <param name="userId">Owner of the conversation; a conversation belonging to anyone else is never compacted.</param>
    /// <param name="minMessages">Minimum message count required before compaction runs.</param>
    void TriggerConversationCompaction(string conversationId, string userId, int minMessages);

    /// <summary>
    /// Fire-and-forget reflection trigger for any caller that observes a turn going wrong outside the
    /// post-turn hook — a user correction, a verification failure. Kept here rather than in each caller
    /// so none of them has to run an LLM call inside its own request scope or make the user wait for it.
    /// </summary>
    /// <param name="request">What went wrong and what the lesson should be scoped to.</param>
    void TriggerReflection(TurnReflectionRequest request);
}
