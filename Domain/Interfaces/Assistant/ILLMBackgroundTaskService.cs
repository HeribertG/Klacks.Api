// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Service for asynchronous background tasks after LLM interactions.
/// </summary>
public interface ILLMBackgroundTaskService
{
    /// <summary>
    /// Post-turn hooks of one chat turn: compaction, skill-execution audit, trajectory capture and, on a
    /// failed call, reflection always run; memory extraction, learning-case collection and grounding read
    /// the answer text and are skipped when that text is a canned empty-answer notice.
    /// </summary>
    /// <param name="agent">The default agent, null when none exists.</param>
    /// <param name="conversation">The conversation the turn belongs to.</param>
    /// <param name="context">The turn context.</param>
    /// <param name="responseContent">The answer as stored.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="answeredWithNotice">True when the answer is an empty-answer notice rather than a model answer.</param>
    void RunBackgroundTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> allFunctionCalls, bool answeredWithNotice = false);

    /// <summary>
    /// Post-turn hooks of a turn the user stopped or whose connection dropped. Only what is safe to draw from
    /// an unfinished turn runs: compaction, the skill-execution audit of the calls that really ran, and the
    /// trajectory capture, which records the turn as interrupted and counts it for nothing. Memory extraction,
    /// learning-case collection, grounding and reflection do not run - they would learn from an answer that
    /// was cut off.
    /// </summary>
    /// <param name="agent">The default agent, null when none exists.</param>
    /// <param name="conversation">The conversation the turn belongs to.</param>
    /// <param name="context">The turn context.</param>
    /// <param name="responseContent">The answer as stored, marker included.</param>
    /// <param name="executedCalls">Only the calls the server ran to their end.</param>
    /// <param name="interruptedPhase">The phase the turn was in, one of InterruptedTurnPhases.</param>
    void RunStoppedTurnTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> executedCalls, string interruptedPhase);

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
