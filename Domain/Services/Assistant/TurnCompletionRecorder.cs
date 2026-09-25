// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence tail of a streamed turn: conversation history, usage row, correction anchor and the post-turn
/// background tasks - for a turn that ran to its end (RecordCompletedAsync) and for one the user stopped or
/// whose connection dropped (RecordStoppedAsync, read from the turn's run state), and for one that ended on an
/// error after the server had run a write action (RecordErroredAsync). A failure is logged and never thrown,
/// because the answer has already been streamed to the user and a storage error must not turn it into a
/// failed turn.
/// </summary>
/// <param name="logger">Logs a storage failure</param>
/// <param name="conversationManager">Writes the history and the usage row</param>
/// <param name="turnPreparation">Records the last-action anchor a later correction reads</param>
/// <param name="agentRepository">Resolves the default agent the background tasks run for</param>
/// <param name="backgroundTaskService">Starts the post-turn background tasks</param>
/// <param name="turnState">The turn's run state, whose outcome is claimed before anything is written</param>
/// <param name="stoppedTurnCleanup">Drops the confirmations a stopped turn issued and closes its UiAction rows</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class TurnCompletionRecorder
{
    private const string HistoryPart = "history";
    private const string UsagePart = "usage row";
    private const string AnchorPart = "correction anchor";

    private readonly ILogger<TurnCompletionRecorder> _logger;
    private readonly LLMConversationManager _conversationManager;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly IAgentRepository _agentRepository;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly TurnRunState _turnState;
    private readonly IStoppedTurnCleanup _stoppedTurnCleanup;

    public TurnCompletionRecorder(
        ILogger<TurnCompletionRecorder> logger,
        LLMConversationManager conversationManager,
        ITurnPreparationService turnPreparation,
        IAgentRepository agentRepository,
        ILLMBackgroundTaskService backgroundTaskService,
        TurnRunState turnState,
        IStoppedTurnCleanup stoppedTurnCleanup)
    {
        _logger = logger;
        _conversationManager = conversationManager;
        _turnPreparation = turnPreparation;
        _agentRepository = agentRepository;
        _backgroundTaskService = backgroundTaskService;
        _turnState = turnState;
        _stoppedTurnCleanup = stoppedTurnCleanup;
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

    /// <summary>
    /// Persists a turn that ended on the user's stop, or whose connection dropped, from the turn's run state.
    /// The outcome is claimed first, so the stop tail and the safety net can both call this and only one
    /// writes. Then, in this order: the history with the partial answer and the interruption marker, the usage
    /// row (only the calls the server ran are named), the correction anchor for exactly those calls, the
    /// confirmations the turn issued and its UiAction rows, and the background tasks that stay allowed for a
    /// turn the user cut off. Every part is best effort and independent of the others. The history rows carry the
    /// time the turn began, and the anchor is left alone when a newer turn has already written or superseded
    /// it, so a turn persisted after the user moved on cannot read as the latest one. A turn that never got as
    /// far as a conversation is only cleaned up.
    /// </summary>
    /// <param name="cancellationToken">Cancels the default-agent lookup; the writes themselves never are</param>
    /// <returns>What the client is told about the write actions that ran</returns>
    public async Task<StoppedTurnSummary> RecordStoppedAsync(CancellationToken cancellationToken)
    {
        var context = _turnState.Context;
        var summary = StoppedTurnSummary.From(context, _turnState.Calls);

        if (!_turnState.TrySetOutcome(TurnOutcome.Stopped))
        {
            _logger.LogWarning(
                "Turn {TurnId} already has outcome {Outcome}; the stopped turn is not persisted again",
                context?.TurnId, _turnState.Outcome);
            return summary;
        }

        if (context == null)
        {
            return summary;
        }

        await PersistCutOffTurnAsync(context, TurnInterruptionDefaults.InterruptedMarker, hasError: false, cancellationToken);
        return summary;
    }

    /// <summary>
    /// Persists a turn whose outcome is Errored, but only when the server had already run a write action in it:
    /// that action stays in place, so the turn must leave its history, usage and correction anchor behind the
    /// way a stopped turn does, or a "no, I meant..." that follows could not find what was done. It is stored
    /// under a neutral error marker, not the user's, and the background tasks are the same reduced set a stop
    /// gets: an error teaches nothing. A turn without such a write is left exactly as it was, and so is one
    /// whose persistence was already claimed, which is what keeps the chat service and the safety net from
    /// both writing it. A failure is logged and never thrown.
    /// </summary>
    /// <param name="cancellationToken">Cancels the default-agent lookup; the writes themselves never are</param>
    public async Task RecordErroredAsync(CancellationToken cancellationToken)
    {
        var context = _turnState.Context;
        if (_turnState.Outcome != TurnOutcome.Errored
            || context == null
            || StoppedTurnSummary.From(context, _turnState.Calls).ExecutedCount == 0)
        {
            return;
        }

        if (!_turnState.TryClaimErroredPersistence())
        {
            _logger.LogWarning(
                "Turn {TurnId} that ended on an error is already persisted; it is not persisted again", context.TurnId);
            return;
        }

        try
        {
            await PersistCutOffTurnAsync(context, TurnInterruptionDefaults.ErroredMarker, hasError: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting the turn of user {UserId} that ended on an error", context.UserId);
        }
    }

    private async Task PersistCutOffTurnAsync(
        LLMContext context, string marker, bool hasError, CancellationToken cancellationToken)
    {
        var conversation = _turnState.Conversation;
        var model = _turnState.Model;
        var executedCalls = StoppedTurnSummary.ExecutedCalls(_turnState.Calls);
        var storedAnswer = StoppedTurnSummary.StoredAnswer(_turnState.StreamedContent.ToString(), marker);
        var phase = _turnState.Phase;

        if (conversation != null && model != null)
        {
            await TryRecordAsync(HistoryPart, context.UserId, () => _conversationManager.SaveConversationMessagesAsync(
                conversation, context.Message, storedAnswer, model.ModelId, _turnState.StartedAtUtc));

            await TryRecordAsync(UsagePart, context.UserId, () => _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation, _turnState.Usage, _turnState.ElapsedMs,
                hasError: hasError,
                errorMessage: hasError ? TurnInterruptionDefaults.ErroredUsageMessage : null,
                ttftMs: _turnState.TtftMs, toolsetAssemblyMs: context.ToolsetAssemblyMs,
                toolIterations: _turnState.ToolIterations,
                turnId: context.TurnId, functionsCalledJson: LLMService.SerializeFunctionsCalled(executedCalls),
                toolChoiceRequested: _turnState.ToolChoiceRequested,
                toolChoiceSupported: _turnState.ProviderSupportsToolChoice,
                toolCallReturned: _turnState.Calls.Count > 0));

            await TryRecordAsync(AnchorPart, context.UserId, () =>
            {
                RecordStoppedTurnAnchor(context, conversation.ConversationId, storedAnswer, executedCalls);
                return Task.CompletedTask;
            });
        }

        await _stoppedTurnCleanup.CleanUpAsync(context.UserId, context.TurnId.GetValueOrDefault(), CancellationToken.None);

        if (conversation != null)
        {
            try
            {
                var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
                _backgroundTaskService.RunStoppedTurnTasks(agent, conversation, context, storedAnswer, executedCalls, phase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting the background tasks of a cut-off turn for user {UserId}", context.UserId);
            }
        }
    }

    private void RecordStoppedTurnAnchor(
        LLMContext context, string conversationId, string storedAnswer, IReadOnlyList<LLMFunctionCall> executedCalls)
    {
        if (_turnPreparation.HasLastActionSince(context, conversationId, _turnState.StartedAtUtc))
        {
            _logger.LogInformation(
                "Turn {TurnId} was persisted after a newer turn wrote the correction anchor of conversation {ConversationId}; the anchor is kept",
                context.TurnId, conversationId);
            return;
        }

        _turnPreparation.RecordLastAction(context, conversationId, storedAnswer, executedCalls, context.RecipePausedOnAsk);
    }

    private async Task TryRecordAsync(string part, string userId, Func<Task> write)
    {
        try
        {
            await write();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving the {Part} of a stopped stream turn for user {UserId}", part, userId);
        }
    }
}
