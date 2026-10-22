// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The tool loop of a non-streaming chat turn: per iteration a recipe confirmation or ask step, or a model
/// call with the turn's toolset whose tool calls are run and fed back, then the closing with the
/// empty-answer recovery, the deterministic re-ask after a topic switch, the recipe run bookkeeping and the
/// closing notices. Nothing reaches the client before the loop ends, so a false success claim can still be
/// retried once with a forcing nudge. One instance serves the turns of one chat service.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
/// <param name="conversationManager">Tracks the usage of a turn the provider failed</param>
/// <param name="functionExecutor">Runs the tool calls and reports what kind of calls they were</param>
/// <param name="recipeEngine">Resolves the recipe the turn runs</param>
/// <param name="recipeRunRecorder">Records the recipe run</param>
/// <param name="turnPreparation">Prepares the turn's recipe state</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class MultiTurnChatLoop
{
    internal const string ProviderErrorFallback = "An error occurred.";

    private readonly ILogger _logger;
    private readonly LLMConversationManager _conversationManager;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IRecipeRunRecorder _recipeRunRecorder;
    private readonly ITurnPreparationService _turnPreparation;

    internal MultiTurnChatLoop(
        ILogger logger,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        ITurnPreparationService turnPreparation)
    {
        _logger = logger;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _turnPreparation = turnPreparation;
    }

    /// <param name="ctx">The prepared turn; receives the tool-choice, paused-on-ask and notice flags</param>
    internal async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls, string? askedSlot)> ExecuteAsync(
        MultiTurnContext ctx)
    {
        var runningHistory = new List<Providers.LLMMessage>(ctx.TruncatedHistory);
        var historyBudget = ConversationHistoryBudget.HistoryBudgetFor(
            ctx.Provider, ctx.Model, ctx.SystemPrompt, ctx.VolatilePrompt, ctx.Context.AvailableFunctions);
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(ctx.Context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(ctx.Context.Message);
        // RecipeTurnState.BeginAsync writes the plan's name onto the shared context object rather than
        // returning it: ProcessAsync holds the very same LLMContext instance and hands it to the post-turn
        // hooks, so the name reaches trajectory capture without widening this method's already six-wide
        // return tuple.
        var recipe = await RecipeTurnState.BeginAsync(
            _turnPreparation, _recipeRunRecorder, _recipeEngine, _logger,
            ctx.Context, ctx.Provider, ctx.Model, ctx.Conversation.ConversationId, ctx.CancellationToken);
        var state = new MultiTurnLoopState(
            runningHistory, ctx.Context.Message, historyBudget, isMutationIntent, isNavigationIntent, recipe);

        for (int iteration = 0; iteration < LLMLoopConstants.MaxChatToolIterations; iteration++)
        {
            var outcome = await RunIterationAsync(ctx, state, iteration);
            if (outcome == ChatIterationOutcome.EndTurn)
            {
                return (state.LastResponse!.Error ?? ProviderErrorFallback, state.LastResponse, state.IterationsUsed, state.AllFunctionCalls, null);
            }

            if (outcome == ChatIterationOutcome.LeaveLoop)
            {
                break;
            }
        }

        return await CloseAsync(ctx, state);
    }

    private async Task<ChatIterationOutcome> RunIterationAsync(MultiTurnContext ctx, MultiTurnLoopState state, int iteration)
    {
        state.IterationsUsed = iteration + 1;

        ConversationHistoryBudget.FitRunningHistoryToBudget(state.RunningHistory, state.CurrentMessage, state.HistoryBudget);

        if (await TryPauseOnRecipeStepAsync(ctx, state))
        {
            return ChatIterationOutcome.LeaveLoop;
        }

        var (providerRequest, forceRecipe) = BuildIterationRequest(ctx, state, iteration);

        var lastResponse = await TransientProviderRetry.ProcessAsync(ctx.Provider, providerRequest, _logger, ctx.CancellationToken);
        state.LastResponse = lastResponse;
        LLMUsageAccumulator.Add(ctx.TotalUsage, lastResponse.Usage);

        if (!lastResponse.Success)
        {
            await TrackFailedIterationAsync(ctx, state);
            return ChatIterationOutcome.EndTurn;
        }

        state.ResponseContent = lastResponse.Content;

        if (!lastResponse.FunctionCalls.Any())
        {
            return NudgeOrLeave(state, iteration);
        }

        return await RunToolCallsAsync(ctx, state, forceRecipe);
    }

    private async Task<bool> TryPauseOnRecipeStepAsync(MultiTurnContext ctx, MultiTurnLoopState state)
    {
        var enginePlan = state.EnginePlan;
        var recipe = state.Recipe;

        enginePlan?.AdvanceOverSatisfied();
        if (enginePlan != null && enginePlan.NeedsConfirmation)
        {
            var proposal = enginePlan.DeterministicConfirmationText(ctx.Context.Language);
            state.LastResponse = proposal != null ? new LLMProviderResponse { Content = proposal }
                : await TransientProviderRetry.ProcessAsync(ctx.Provider, LLMProviderRequestFactory.RecipeStep(
                    ctx.Model, state.CurrentMessage, ctx.SystemPrompt, ctx.VolatilePrompt, enginePlan.ConfirmationInstruction, state.RunningHistory), _logger, ctx.CancellationToken);
            LLMUsageAccumulator.Add(ctx.TotalUsage, state.LastResponse.Usage);
            if (state.LastResponse.Success)
            {
                state.ResponseContent = proposal ?? RecipeReplyGuard.WithConfirmationChip(RecipeReplyGuard.SafeConfirmation(
                    state.LastResponse.Content, enginePlan.Goal, enginePlan.AlternativeGoal, ctx.Context.Language,
                    enginePlan.GoalTranslations, enginePlan.AlternativeGoalTranslations), enginePlan.AlternativeGoal, ctx.Context.Language);
            }

            await recipe.PauseForConfirmationAsync(ctx.CancellationToken);
            return true;
        }

        if (enginePlan != null && enginePlan.IsActive && enginePlan.CurrentIsAsk && !enginePlan.TopicSwitchThisTurn)
        {
            var askInstruction = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                RecipeEngineDefaults.AskStepInstructionTemplate, enginePlan.CurrentAskPrompt);
            var askRequest = LLMProviderRequestFactory.RecipeStep(
                ctx.Model, state.CurrentMessage, ctx.SystemPrompt, ctx.VolatilePrompt, askInstruction, state.RunningHistory);

            state.LastResponse = await TransientProviderRetry.ProcessAsync(ctx.Provider, askRequest, _logger, ctx.CancellationToken);
            LLMUsageAccumulator.Add(ctx.TotalUsage, state.LastResponse.Usage);
            if (state.LastResponse.Success)
            {
                state.ResponseContent = RecipeReplyGuard.SafeAsk(
                    state.LastResponse.Content, enginePlan.CurrentAskPrompt ?? string.Empty,
                    enginePlan.CurrentAskPromptTranslations, ctx.Context.Language);
            }

            await recipe.PauseOnAskAsync(ctx.CancellationToken);
            return true;
        }

        return false;
    }

    private (LLMProviderRequest Request, bool ForceRecipe) BuildIterationRequest(MultiTurnContext ctx, MultiTurnLoopState state, int iteration)
    {
        var recipe = state.Recipe;

        // Same stability rule as the streaming loop: the toolset never changes across iterations
        // (prompt-prefix cache), repeats of write skills are rejected at execution time instead.
        var iterationFunctions = ctx.Context.AvailableFunctions;

        var confirmThisIteration = recipe.ForceConfirm && state.AllFunctionCalls.Count == 0;
        if (confirmThisIteration)
        {
            iterationFunctions = new List<LLMFunction> { recipe.ConfirmFunction! };
        }

        var (forceRecipe, recipeFunctions, recipeNote) = recipe.ResolveIteration(
            confirmThisIteration, ctx.Context.AvailableFunctions, iterationFunctions);
        iterationFunctions = recipeFunctions;
        if (forceRecipe)
        {
            _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                recipe.Forcing!.Name, recipe.Forcing.CurrentSkill, iteration);
        }

        var providerRequest = LLMProviderRequestFactory.ForIteration(
            ctx.Model, state.CurrentMessage, ctx.SystemPrompt,
            VolatilePromptComposer.CombineVolatile(ctx.VolatilePrompt, IterationNotePolicy.Select(
                confirmThisIteration, recipe.PendingNote, forceRecipe, recipeNote,
                recipe.SuggestPlan, state.AllFunctionCalls.Count, recipe.CompletionNote, recipe.AmbiguousReplyNote)),
            state.RunningHistory, iterationFunctions,
            ToolChoicePolicy.ResolveToolChoice(forceRecipe, state.IsMutationIntent, state.IsNavigationIntent, recipe.ForceConfirm,
                state.AllFunctionCalls.Count, ctx.Context.TurnIntentVetoesForcedToolCall && !state.ForcedRetryUsed));

        if (providerRequest.ToolChoice == MutationGuardConstants.ToolChoiceRequired)
        {
            ctx.ToolChoiceRequested = true;
        }

        return (providerRequest, forceRecipe);
    }

    private async Task TrackFailedIterationAsync(MultiTurnContext ctx, MultiTurnLoopState state)
    {
        var lastResponse = state.LastResponse!;

        _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
            state.IterationsUsed, lastResponse.Error);
        await _conversationManager.TrackUsageAsync(
            ctx.Context.UserId, ctx.Model, ctx.Conversation,
            ctx.TotalUsage, ctx.Stopwatch.ElapsedMilliseconds,
            hasError: true, errorMessage: lastResponse.Error,
            toolsetAssemblyMs: ctx.Context.ToolsetAssemblyMs, toolIterations: state.IterationsUsed,
            turnId: ctx.Context.TurnId, functionsCalledJson: TurnTelemetry.SerializeFunctionsCalled(state.AllFunctionCalls),
            toolChoiceRequested: ctx.ToolChoiceRequested,
            toolChoiceSupported: ctx.Provider.SupportsToolChoice,
            toolCallReturned: state.AllFunctionCalls.Count > 0);
    }

    private static ChatIterationOutcome NudgeOrLeave(MultiTurnLoopState state, int iteration)
    {
        var lastResponse = state.LastResponse!;
        var recipe = state.Recipe;

        // V1 (non-streaming): nothing is sent to the client until the loop ends, so a false
        // success claim can still be suppressed. If a mutation request produced no tool call,
        // retry ONCE with a forcing nudge (the next request sets tool_choice="required" because
        // allFunctionCalls is still empty) before giving up. Also trigger when intent detection
        // missed the phrasing but the model emitted a text tool-call itself (never executes).
        if (ForceToolNudgePolicy.ShouldForceToolNudge(
                state.IsMutationIntent, recipe.ForceConfirm, ToolCallMarkupSanitizer.ContainsMarkup(lastResponse.Content),
                CompletionClaimDetector.ClaimsCompletion(lastResponse.Content),
                state.AllFunctionCalls.Count, recipe.PausedOnAsk, ClarifyingResponse.IsClarifying(lastResponse.Content),
                ClaimNegationDetector.DeniesCompletion(lastResponse.Content))
            && !state.ForcedRetryUsed
            && iteration < LLMLoopConstants.MaxChatToolIterations - 1)
        {
            state.ForcedRetryUsed = true;
            state.RunningHistory.Add(new Providers.LLMMessage { Role = LLMMessageRoles.User, Content = state.CurrentMessage });
            state.RunningHistory.Add(new Providers.LLMMessage
            {
                Role = LLMMessageRoles.Assistant,
                Content = string.IsNullOrWhiteSpace(lastResponse.Content)
                    ? LLMLoopConstants.NoActionHistoryNote
                    : lastResponse.Content
            });
            state.CurrentMessage = MutationGuardConstants.ForceToolNudge;
            return ChatIterationOutcome.NextIteration;
        }

        return ChatIterationOutcome.LeaveLoop;
    }

    private async Task<ChatIterationOutcome> RunToolCallsAsync(MultiTurnContext ctx, MultiTurnLoopState state, bool forceRecipe)
    {
        var lastResponse = state.LastResponse!;
        var recipe = state.Recipe;

        _logger.LogInformation("Multi-turn iteration {Iteration}: executing {Count} function calls",
            state.IterationsUsed, lastResponse.FunctionCalls.Count);

        state.AllFunctionCalls.AddRange(lastResponse.FunctionCalls);
        ToolCallLoopSteps.ApplyRecipeInjections(recipe.Forcing, lastResponse.FunctionCalls);

        var executableCalls = recipe.RejectWrites(RepeatedWriteCallGuard.RejectAndRecord(
            ToolCallLoopSteps.InToolset(lastResponse.FunctionCalls, ctx.Context, _logger), state.CalledFunctionNames, forceRecipe));

        await _functionExecutor.ProcessFunctionCallsAsync(ctx.Context, executableCalls);
        recipe.ObserveExecuted(lastResponse.FunctionCalls);
        if (lastResponse.FunctionCalls.Any(c => c.RequiresConfirmation))
        {
            recipe.ReleaseOnAutonomyGateHold();
        }

        if (executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls)
        {
            _logger.LogInformation("All function calls are UiPassthrough - breaking multi-turn loop");
            return ChatIterationOutcome.LeaveLoop;
        }

        state.RunningHistory.Add(new Providers.LLMMessage { Role = LLMMessageRoles.User, Content = state.CurrentMessage });
        state.RunningHistory.Add(new Providers.LLMMessage
            { Role = LLMMessageRoles.Assistant, Content = AnswerPlaceholder.ForToolCallTurn(lastResponse.Content, lastResponse.FunctionCalls) });
        state.CurrentMessage = ToolCallLoopSteps.FormatFunctionResults(lastResponse.FunctionCalls, ctx.BudgetProfile?.MaxToolResultChars)
            + recipe.TakeGateHoldNote();
        return ChatIterationOutcome.NextIteration;
    }

    private async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls, string? askedSlot)> CloseAsync(
        MultiTurnContext ctx, MultiTurnLoopState state)
    {
        var recipe = state.Recipe;
        var enginePlan = state.EnginePlan;
        var allFunctionCalls = state.AllFunctionCalls;

        var responseContent = await RecoverAnswerAsync(ctx, state);

        // Mirrors the streaming loop: the turn above ran normally (full toolset) because the user's reply
        // to the pending ask was recognized as an independent question, not a slot answer. The plan is
        // still on the same ask step with the slot untouched, so re-ask it deterministically
        // (RecipeReplyGuard.SafeAsk with no model reply always falls through to the authored translation,
        // no extra model call). A live tool-less re-ask call was tried and reverted: with the answer to the
        // independent question still fresh in the running history, the model reliably ignored the "ask the
        // recipe question" instruction and re-explained the just-answered topic instead (reproduced live
        // twice). The deterministic text does not carry the ask step's [REPLIES:...] chips (documented as a
        // known, reported limitation) but is reliable, which this class of bug cannot trade away.
        if (enginePlan != null && enginePlan.TopicSwitchThisTurn && enginePlan.IsActive && enginePlan.CurrentIsAsk)
        {
            var reaskText = RecipeReplyGuard.SafeAsk(
                null, enginePlan.CurrentAskPrompt ?? string.Empty,
                enginePlan.CurrentAskPromptTranslations, ctx.Context.Language);
            responseContent += RecipeEngineDefaults.TopicSwitchReaskSeparator + reaskText;
            await recipe.PauseOnReaskAsync(ctx.CancellationToken);
        }

        await recipe.FinalizeAsync(ctx.CancellationToken);

        var failedCall = TurnClosingNotices.LastUnrecoveredFailure(allFunctionCalls, responseContent);
        if (failedCall != null)
        {
            _logger.LogWarning(
                "All function calls failed in multi-turn loop; surfacing notice for {FunctionName} (result kind {ResultKind}, {ResultLength} chars)",
                failedCall.FunctionName, failedCall.ResultKind, failedCall.Result.LengthForLog());
            responseContent = TurnClosingNotices.StepFailed(failedCall);
        }

        if (allFunctionCalls.Count > 0)
        {
            _logger.LogInformation("Multi-turn completed: {TotalCalls} function calls in {Iterations} iterations",
                allFunctionCalls.Count, state.IterationsUsed);
        }

        ctx.RecipePausedOnAsk = recipe.PausedOnAsk;

        return (responseContent, state.LastResponse, state.IterationsUsed, allFunctionCalls, recipe.AskedSlot);
    }

    /// <summary>
    /// Non-streaming side of the closing guard. A failed last response (a recipe confirmation or ask call)
    /// turns the whole turn into an error response, so recovering its answer would be a wasted call. A turn
    /// without tool calls recovers against the user's message, never against a force-tool nudge, and
    /// without the nudge's exchange, which would otherwise repeat that message in the history. After a read-only
    /// recipe a completion claim gets the nothing-stored notice, and a turn whose every side-effecting call
    /// failed gets the same notice whatever its answer claims, exactly like on the streaming path.
    /// </summary>
    private async Task<string> RecoverAnswerAsync(MultiTurnContext ctx, MultiTurnLoopState state)
    {
        var responseContent = state.ResponseContent;
        var allFunctionCalls = state.AllFunctionCalls;
        var recipe = state.Recipe;

        if (state.LastResponse is { Success: false })
        {
            return responseContent;
        }

        var noToolRan = allFunctionCalls.Count == 0;
        var recovery = StreamedTurnClosing.RecoveryFor(
            _logger, ctx.Provider, ctx.TotalUsage, ctx.Model, noToolRan ? ctx.Context.Message : state.CurrentMessage, ctx.SystemPrompt, ctx.VolatilePrompt,
            noToolRan && state.ForcedRetryUsed ? ForceToolNudgePolicy.WithoutNudgeExchange(state.RunningHistory, ctx.Context.Message) : state.RunningHistory,
            state.HistoryBudget, ctx.Context.Language);
        var answer = await recovery.ResolveAsync(
            responseContent, allFunctionCalls, () => _functionExecutor.LastBatchWasUiPassthroughOnly, recipe.PausedOnAsk, ctx.CancellationToken);
        ctx.AnsweredWithNotice = recovery.AnsweredWithNotice;
        return answer + TurnClosingNotices.NothingChanged(
            recipe.ReadOnlyRecipeCompleted, answer, allFunctionCalls, ctx.Context.Language, ctx.Context.AvailableFunctions);
    }
}
