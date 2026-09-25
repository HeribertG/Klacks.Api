// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence tail of a finished streamed turn: conversation history, usage row, correction anchor and
/// the post-turn background tasks. A failure is logged and never thrown, because the answer has already
/// been streamed to the user and a storage error must not turn it into a failed turn.
/// </summary>
/// <param name="logger">Logs a storage failure</param>
/// <param name="conversationManager">Writes the history and the usage row</param>
/// <param name="turnPreparation">Records the last-action anchor a later correction reads</param>
/// <param name="agentRepository">Resolves the default agent the background tasks run for</param>
/// <param name="backgroundTaskService">Starts the post-turn background tasks</param>
/// <param name="turnState">The turn's run state, whose outcome is claimed before anything is written</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class TurnCompletionRecorder
{
    private readonly ILogger<TurnCompletionRecorder> _logger;
    private readonly LLMConversationManager _conversationManager;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly IAgentRepository _agentRepository;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly TurnRunState _turnState;

    public TurnCompletionRecorder(
        ILogger<TurnCompletionRecorder> logger,
        LLMConversationManager conversationManager,
        ITurnPreparationService turnPreparation,
        IAgentRepository agentRepository,
        ILLMBackgroundTaskService backgroundTaskService,
        TurnRunState turnState)
    {
        _logger = logger;
        _conversationManager = conversationManager;
        _turnPreparation = turnPreparation;
        _agentRepository = agentRepository;
        _backgroundTaskService = backgroundTaskService;
        _turnState = turnState;
    }

    /// <param name="turn">The finished turn to persist</param>
    /// <param name="cancellationToken">Cancels the default-agent lookup</param>
    public async Task RecordCompletedAsync(TurnCompletion turn, CancellationToken cancellationToken)
    {
        // Claimed before the first write on purpose: a storage failure below must still leave the turn
        // recorded as completed, otherwise the interrupted-turn safety net would store it a second time.
        if (!_turnState.TrySetOutcome(TurnOutcome.Completed))
        {
            _logger.LogWarning(
                "Turn {TurnId} already has outcome {Outcome}; the completed turn is not persisted again",
                turn.Context.TurnId, _turnState.Outcome);
            return;
        }

        try
        {
            await _conversationManager.SaveConversationMessagesAsync(
                turn.Conversation, turn.Context.Message, turn.ResponseContent, turn.Model.ModelId);

            await _conversationManager.TrackUsageAsync(
                turn.Context.UserId, turn.Model, turn.Conversation,
                turn.Usage, turn.ElapsedMs,
                ttftMs: turn.TtftMs, toolsetAssemblyMs: turn.Context.ToolsetAssemblyMs, toolIterations: turn.ToolIterations,
                turnId: turn.Context.TurnId, functionsCalledJson: LLMService.SerializeFunctionsCalled(turn.FunctionCalls),
                toolChoiceRequested: turn.ToolChoiceRequested,
                toolChoiceSupported: turn.ProviderSupportsToolChoice,
                toolCallReturned: turn.FunctionCalls.Count > 0);

            _turnPreparation.RecordLastAction(
                turn.Context, turn.Conversation.ConversationId, turn.ResponseContent, turn.FunctionCalls, turn.RecipePausedOnAsk);

            var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
            _backgroundTaskService.RunBackgroundTasks(
                agent, turn.Conversation, turn.Context, turn.ResponseContent, turn.FunctionCalls, turn.AnsweredWithNotice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stream conversation for user {UserId}", turn.Context.UserId);
        }
    }
}
