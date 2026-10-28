// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that switches every post-turn background hook off on the public Playground: conversation compaction,
/// skill-execution logging, trajectory capture, auto-memory extraction, learning-case collection, answer grounding
/// and reflection. All of them store text derived from visitor input, and every visitor is the same demo admin,
/// so one visitor would leak into the next. Every hook runs through this one interface, which makes it the single
/// seam. With the Playground mode off each call is forwarded unchanged.
/// </summary>
/// <param name="inner">The real background task service</param>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Playground;

public sealed class PlaygroundSilentBackgroundTaskService : ILLMBackgroundTaskService
{
    private readonly ILLMBackgroundTaskService _inner;
    private readonly IPlaygroundMode _mode;

    public PlaygroundSilentBackgroundTaskService(ILLMBackgroundTaskService inner, IPlaygroundMode mode)
    {
        _inner = inner;
        _mode = mode;
    }

    public void RunBackgroundTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> allFunctionCalls, bool answeredWithNotice = false)
    {
        if (!_mode.IsActive)
        {
            _inner.RunBackgroundTasks(agent, conversation, context, responseContent, allFunctionCalls, answeredWithNotice);
        }
    }

    public void RunStoppedTurnTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> executedCalls, string interruptedPhase)
    {
        if (!_mode.IsActive)
        {
            _inner.RunStoppedTurnTasks(agent, conversation, context, responseContent, executedCalls, interruptedPhase);
        }
    }

    public void TriggerConversationCompaction(string conversationId, string userId, int minMessages)
    {
        if (!_mode.IsActive)
        {
            _inner.TriggerConversationCompaction(conversationId, userId, minMessages);
        }
    }

    public void TriggerReflection(TurnReflectionRequest request)
    {
        if (!_mode.IsActive)
        {
            _inner.TriggerReflection(request);
        }
    }
}
