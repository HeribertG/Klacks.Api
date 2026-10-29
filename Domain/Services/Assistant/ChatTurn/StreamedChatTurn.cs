// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The streamed part of a chat turn after its context is prepared and its stream has started: resolves the
/// recipe, runs the tool loop iteration by iteration (recipe pause step, model call, tool round), closes the
/// turn and persists it, then streams the metadata and the end of the stream. A model error ends the stream
/// with an error chunk and a call cancelled without a stop request ends it silently; a stop request, an
/// answer without tool calls and a tool round that ends the turn go on to the closing. A stop request is
/// answered by the stopped-turn tail. One instance serves the turns of one chat service.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
/// <param name="functionExecutor">Runs the tool calls and reports what kind of calls they were</param>
/// <param name="responseBuilder">Builds the metadata response</param>
/// <param name="recipeEngine">Resolves the recipe the turn runs</param>
/// <param name="recipeRunRecorder">Records the recipe run</param>
/// <param name="turnPreparation">Prepares the turn's recipe state</param>
/// <param name="turnCompletionRecorder">Persists the completed or stopped turn</param>
/// <param name="suggestionGrounding">Grounds the metadata's suggestion chips</param>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class StreamedChatTurn
{

    private readonly ILogger _logger;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly LLMResponseBuilder _responseBuilder;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IRecipeRunRecorder _recipeRunRecorder;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly TurnCompletionRecorder _turnCompletionRecorder;
    private readonly SuggestionGrounding _suggestionGrounding;

    internal StreamedChatTurn(
        ILogger logger,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        ITurnPreparationService turnPreparation,
        TurnCompletionRecorder turnCompletionRecorder,
        SuggestionGrounding suggestionGrounding)
    {
        _logger = logger;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _turnPreparation = turnPreparation;
        _turnCompletionRecorder = turnCompletionRecorder;
        _suggestionGrounding = suggestionGrounding;
    }

    /// <param name="turn">The turn's run state, begun for this context; the conversation is attached to it here</param>
    /// <param name="context">The turn context</param>
    /// <param name="prepared">The successfully prepared turn context</param>
    /// <param name="stopwatch">The turn clock started on entry</param>
    /// <param name="cancellationToken">Cancels the recipe resolve, the model calls and the closing</param>
    internal async IAsyncEnumerable<SseChunk> StreamAsync(
        TurnRunState turn,
        LLMContext context,
        PreparedChatTurn prepared,
        Stopwatch stopwatch,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var model = prepared.Model!;
        var provider = prepared.Provider!;
        var conversation = prepared.Conversation!;

        turn.Attach(conversation, model, provider.SupportsToolChoice);
        var runningHistory = new List<Providers.LLMMessage>(prepared.History!);
        var historyBudget = ConversationHistoryBudget.HistoryBudgetFor(
            provider, model, prepared.SystemPrompt, prepared.VolatilePrompt, context.AvailableFunctions);
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(context.Message);

        if (turn.StopRequested)
        {
            await foreach (var stoppedChunk in StoppedTurnTail.StreamAsync(_turnCompletionRecorder, turn))
            {
                yield return stoppedChunk;
            }

            yield break;
        }

        // Emitted unconditionally, not only when a recipe turns out to be active: the resolve itself
        // runs on every turn (recipe table read, trigger matching, semantic fallback and slot
        // extraction — the last of which is a model call), so the wait is real regardless of outcome.
        yield return SseChunk.Status(SseStatusStages.ResolvingRecipe, TurnTelemetry.ElapsedMsFor(context));

        var recipe = await RecipeTurnState.BeginAsync(
            _turnPreparation, _recipeRunRecorder, _recipeEngine, _logger,
            context, provider, model, conversation.ConversationId, cancellationToken);
        var state = new StreamedChatLoopState(
            runningHistory, context.Message, historyBudget, isMutationIntent, isNavigationIntent, recipe);

        for (int iteration = 0; iteration < LLMLoopConstants.MaxChatToolIterations; iteration++)
        {
            await foreach (var iterationChunk in StreamIterationAsync(
                               turn, context, prepared, state, stopwatch, iteration, cancellationToken))
            {
                yield return iterationChunk;
            }

            if (state.Outcome == ChatIterationOutcome.EndTurn)
            {
                yield break;
            }

            if (state.Outcome == ChatIterationOutcome.LeaveLoop)
            {
                break;
            }
        }

        var closing = new StreamedTurnClosing(_logger, _functionExecutor);
        await foreach (var closingChunk in closing.StreamAsync(
                           turn,
                           recipe,
                           new TurnClosingInput(
                               provider, model, state.CurrentMessage, prepared.SystemPrompt!, prepared.VolatilePrompt,
                               state.RunningHistory, state.HistoryBudget, state.LastCallStart, state.IsMutationIntent),
                           cancellationToken))
        {
            yield return closingChunk;
        }

        if (turn.StopRequested)
        {
            await foreach (var stoppedChunk in StoppedTurnTail.StreamAsync(_turnCompletionRecorder, turn))
            {
                yield return stoppedChunk;
            }

            yield break;
        }

        var responseContent = turn.StreamedContent.ToString();

        await _turnCompletionRecorder.RecordCompletedAsync(
            new TurnCompletion(
                context, conversation, model, provider.SupportsToolChoice, responseContent, turn.Usage,
                stopwatch.ElapsedMilliseconds, turn.TtftMs, turn.ToolIterations, turn.Calls,
                turn.ToolChoiceRequested, recipe.PausedOnAsk, turn.AnsweredWithNotice),
            CancellationToken.None);

        var metadataResponse = _responseBuilder.BuildSuccessResponse(
            new LLMProviderResponse { Content = responseContent, Usage = turn.Usage, Success = true },
            conversation.ConversationId, responseContent, turn.Calls, turn.NavigationRoute, turn.NavigationTarget);
        await _suggestionGrounding.ApplyAsync(metadataResponse, recipe.AskedSlot, cancellationToken);

        yield return SseChunk.Metadata(metadataResponse);
        yield return SseChunk.Done();
    }

    /// <summary>
    /// One iteration of the tool loop. Leaves state.Outcome at LeaveLoop when the turn goes on to its
    /// closing, sets EndTurn when the stream ends here and NextIteration when the tool results go back to
    /// the model.
    /// </summary>
    private async IAsyncEnumerable<SseChunk> StreamIterationAsync(
        TurnRunState turn,
        LLMContext context,
        PreparedChatTurn prepared,
        StreamedChatLoopState state,
        Stopwatch stopwatch,
        int iteration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var model = prepared.Model!;
        var provider = prepared.Provider!;
        var recipe = state.Recipe;
        state.Outcome = ChatIterationOutcome.LeaveLoop;

        if (turn.StopRequested)
        {
            yield break;
        }

        turn.ToolIterations = iteration + 1;
        state.LastCallStart = turn.StreamedContent.Length;
        ConversationHistoryBudget.FitRunningHistoryToBudget(state.RunningHistory, state.CurrentMessage, state.HistoryBudget);

        await foreach (var stepChunk in RecipePauseStep.StreamAsync(
                           _logger, recipe, provider, model, state.CurrentMessage, prepared.SystemPrompt!, prepared.VolatilePrompt,
                           state.RunningHistory, turn, cancellationToken))
        {
            yield return stepChunk;
        }

        if (recipe.PausedOnAsk || turn.StopRequested)
        {
            yield break;
        }

        var (providerRequest, forceRecipe) = BuildIterationRequest(turn, context, prepared, state, iteration);

        yield return SseChunk.Status(SseStatusStages.CallingModel, TurnTelemetry.ElapsedMsFor(context), turn.ToolIterations);

        var modelCall = new StreamedModelCall(_logger);
        await foreach (var callChunk in modelCall.RunAsync(
                           provider, providerRequest, model, turn, stopwatch, cancellationToken))
        {
            yield return callChunk;
        }

        if (modelCall.Error != null)
        {
            turn.TrySetOutcome(TurnOutcome.Errored);
            state.Outcome = ChatIterationOutcome.EndTurn;
            yield return SseChunk.Error(modelCall.Error);
            yield break;
        }

        var accumulator = modelCall.Accumulator;

        if (modelCall.Cancelled && !turn.StopRequested)
        {
            state.Outcome = ChatIterationOutcome.EndTurn;
            yield break;
        }

        if (turn.StopRequested || !accumulator.HasFunctionCalls)
        {
            yield break;
        }

        var functionCalls = accumulator.FunctionCalls.ToList();
        turn.RegisterCalls(functionCalls);
        ToolCallLoopSteps.ApplyRecipeInjections(recipe.Forcing, functionCalls, context.AvailableFunctions, _logger);

        var executableCalls = recipe.RejectWrites(RepeatedWriteCallGuard.RejectAndRecord(
            ToolCallLoopSteps.InToolset(functionCalls, context, _logger), state.CalledFunctionNames, forceRecipe));

        var toolRound = new StreamedToolRound(_functionExecutor);
        await foreach (var roundChunk in toolRound.RunAsync(turn, recipe, functionCalls, executableCalls))
        {
            yield return roundChunk;
        }

        if (toolRound.EndsTurn || turn.StopRequested)
        {
            yield break;
        }

        state.RunningHistory.Add(new Providers.LLMMessage { Role = LLMMessageRoles.User, Content = state.CurrentMessage });
        state.RunningHistory.Add(new Providers.LLMMessage
            { Role = LLMMessageRoles.Assistant, Content = AnswerPlaceholder.ForToolCallTurn(accumulator.AccumulatedContent, functionCalls) });
        state.CurrentMessage = ToolCallLoopSteps.FormatFunctionResults(functionCalls, prepared.BudgetProfile?.MaxToolResultChars)
            + recipe.TakeGateHoldNote();
        state.Outcome = ChatIterationOutcome.NextIteration;
    }

    private (LLMProviderRequest Request, bool ForceRecipe) BuildIterationRequest(
        TurnRunState turn, LLMContext context, PreparedChatTurn prepared, StreamedChatLoopState state, int iteration)
    {
        var recipe = state.Recipe;

        // The full toolset is sent on every iteration (except when it is deliberately narrowed
        // for a forced confirmation or a recipe-forcing step below). Shrinking it per iteration
        // (the previous behaviour) changed the tool array mid-turn and invalidated the provider's
        // prompt-prefix cache on every follow-up call — for every provider with prefix caching,
        // not just one. The once-per-turn rule for write skills is enforced at execution time
        // instead (RepeatedWriteCallGuard), which is also stricter: a hallucinated repeat call
        // is now rejected rather than silently executed.
        var iterationFunctions = context.AvailableFunctions;

        var confirmThisIteration = recipe.ForceConfirm && turn.Calls.Count == 0;
        if (confirmThisIteration)
        {
            iterationFunctions = new List<LLMFunction> { recipe.ConfirmFunction! };
        }

        var (forceRecipe, recipeFunctions, recipeNote) = recipe.ResolveIteration(
            confirmThisIteration, context.AvailableFunctions, iterationFunctions);
        iterationFunctions = recipeFunctions;
        if (forceRecipe)
        {
            _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                recipe.Forcing!.Name, recipe.Forcing.CurrentSkill, iteration);
        }

        var providerRequest = LLMProviderRequestFactory.ForIteration(
            prepared.Model!, state.CurrentMessage, prepared.SystemPrompt!,
            VolatilePromptComposer.CombineVolatile(prepared.VolatilePrompt, IterationNotePolicy.Select(
                confirmThisIteration, recipe.PendingNote, forceRecipe, recipeNote,
                recipe.SuggestPlan, turn.Calls.Count, recipe.CompletionNote, recipe.AmbiguousReplyNote)),
            state.RunningHistory, iterationFunctions,
            ToolChoicePolicy.ResolveToolChoice(forceRecipe, state.IsMutationIntent, state.IsNavigationIntent, recipe.ForceConfirm,
                turn.Calls.Count, context.TurnIntentVetoesForcedToolCall),
            stream: true,
            onStreamUsage: usage => LLMUsageAccumulator.Add(turn.Usage, usage));

        if (providerRequest.ToolChoice == MutationGuardConstants.ToolChoiceRequired)
        {
            turn.ToolChoiceRequested = true;
        }

        return (providerRequest, forceRecipe);
    }
}
