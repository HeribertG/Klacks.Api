// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers a graceful-correction turn with its authored clarification question instead of calling the
/// model: persists the turn and builds its response payload. Shared by both chat paths so the streaming and
/// the non-streaming chat persist exactly the same turn.
/// </summary>
/// <param name="conversationManager">Saves the turn's messages and tracks its usage</param>
/// <param name="agentRepository">Supplies the default agent for the post-turn background tasks</param>
/// <param name="backgroundTaskService">Runs the post-turn background tasks</param>
/// <param name="responseBuilder">Builds the response payload</param>

using System.Diagnostics;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class ClarificationTurnResponder
{
    private readonly LLMConversationManager _conversationManager;
    private readonly IAgentRepository _agentRepository;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly LLMResponseBuilder _responseBuilder;

    internal ClarificationTurnResponder(
        LLMConversationManager conversationManager,
        IAgentRepository agentRepository,
        ILLMBackgroundTaskService backgroundTaskService,
        LLMResponseBuilder responseBuilder)
    {
        _conversationManager = conversationManager;
        _agentRepository = agentRepository;
        _backgroundTaskService = backgroundTaskService;
        _responseBuilder = responseBuilder;
    }

    /// <summary>
    /// The save/track/background tail of a turn that answered with the authored clarification question of
    /// a graceful correction instead of calling the model. Usage is tracked with an empty usage record
    /// because no provider was called at all, and the background tasks run with an empty call list, so the
    /// turn is captured as what it was: a correction answered with a question and no action.
    /// RecordLastAction is deliberately NOT called: this turn executed nothing, so there is nothing to
    /// record, and calling it would only redundantly mark the record superseded. The pins the entry point
    /// wrote onto that record are not at risk either way - a superseded record still carries them.
    /// </summary>
    internal async Task PersistAsync(
        LLMContext context,
        LLMConversation conversation,
        LLMModel model,
        ILLMProvider provider,
        string clarification,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        await _conversationManager.SaveConversationMessagesAsync(
            conversation, context.Message, clarification, model.ModelId);

        await _conversationManager.TrackUsageAsync(
            context.UserId, model, conversation,
            new Providers.LLMUsage(), stopwatch.ElapsedMilliseconds,
            toolsetAssemblyMs: context.ToolsetAssemblyMs, toolIterations: 0,
            turnId: context.TurnId, functionsCalledJson: null,
            toolChoiceRequested: false, toolChoiceSupported: provider.SupportsToolChoice,
            toolCallReturned: false);

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        _backgroundTaskService.RunBackgroundTasks(
            agent, conversation, context, clarification, new List<LLMFunctionCall>());
    }

    /// <summary>
    /// The response payload of a clarification turn: the authored question as content, an empty usage
    /// record and no function calls, since nothing was called and nothing was navigated to.
    /// </summary>
    internal LLMResponse BuildResponse(LLMConversation conversation, string clarification)
    {
        return _responseBuilder.BuildSuccessResponse(
            new LLMProviderResponse { Content = clarification, Usage = new Providers.LLMUsage(), Success = true },
            conversation.ConversationId, clarification, new List<LLMFunctionCall>(), null, null);
    }
}
