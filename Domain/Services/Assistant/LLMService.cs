// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMService : ILLMService
{
    private readonly ILogger<LLMService> _logger;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly LLMConversationManager _conversationManager;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly LLMResponseBuilder _responseBuilder;
    private readonly LLMSystemPromptBuilder _promptBuilder;
    private readonly IAgentRepository _agentRepository;
    private readonly ContextAssemblyPipeline _contextAssemblyPipeline;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IRecipeRunRecorder _recipeRunRecorder;
    private readonly ISuggestionEntityNameReader _suggestionEntityNameReader;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly TurnCompletionRecorder _turnCompletionRecorder;
    private readonly TurnRunState _turnState;

    private const int MaxHistoryMessages = 20;

    // Rough characters-per-token ratio used for all local prompt-size estimates. Deliberately
    // low (conservative) so estimates over- rather than under-count real tokenizer output.
    // Internal (not private): referenced by LLMServiceHistoryBudgetTests so the test math derives
    // from the same source of truth instead of duplicating the ratio as a test-side magic number.
    internal const int CharsPerToken = 4;

    // Percentage buffer added on top of the measured tool-definition size (see
    // EstimateToolDefinitionReserveTokens). Absorbs: (1) drift between the CharsPerToken=4 heuristic
    // and each provider's real tokenizer, (2) provider-specific wrapper overhead not modeled by the
    // generic {name, description, parameters:{type, properties, required}} shape used for the estimate
    // (e.g. OpenAI's outer {"type":"function","function":{...}} envelope, Anthropic cache_control
    // blocks). 30% keeps every measured tier (Tier 12 ~3k, Tier 15 ~3.7k, Tier 30 ~6.9k raw tokens) far
    // under the old flat 15k reserve, while still pushing the reserve for the theoretical worst case
    // (30 maximum-sized catalog skills, ~16k raw tokens) above 15k - the one scenario where the old
    // flat constant was already known to be insufficient.
    internal const int ToolDefinitionSafetyMarginPercent = 30;

    // Headroom deliberately kept free on every turn so that a single recall/list answer cannot fill the
    // whole context window — leaving room for the model's reply and for follow-up interactions.
    internal const int InteractionHeadroomTokens = 8_000;

    // Extra slack absorbing tokenizer/estimate drift (our CharsPerToken estimate is approximate).
    internal const int SafetyMarginTokens = 2_000;

    // Never starve history below this, even if overhead estimates are pessimistic.
    internal const int MinHistoryBudgetTokens = 4_000;

    // Deliberately carries no counts. It is inserted at position 0, i.e. into the prompt prefix every
    // provider with automatic prefix caching (DeepSeek among them) hashes: message counts change from
    // turn to turn and from tool iteration to tool iteration, so a counted notice invalidated the cache
    // on every single call. Internal so tests assert against this exact text instead of copying it.
    internal const string TruncationNotice = "[Earlier messages truncated.]";

    private const int StageLogThresholdMs = 50;

    // Extra budget reserved for the wrapper markers around an injected conversation-summary system message.
    private const int SummaryBudgetReserveTokens = 50;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        IAgentRepository agentRepository,
        ContextAssemblyPipeline contextAssemblyPipeline,
        ILLMBackgroundTaskService backgroundTaskService,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        ISuggestionEntityNameReader suggestionEntityNameReader,
        IContextBudgetPolicy contextBudgetPolicy,
        ITurnPreparationService turnPreparation,
        TurnCompletionRecorder turnCompletionRecorder,
        TurnRunState turnState)
    {
        _logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
        _agentRepository = agentRepository;
        _contextAssemblyPipeline = contextAssemblyPipeline;
        _backgroundTaskService = backgroundTaskService;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _suggestionEntityNameReader = suggestionEntityNameReader;
        _contextBudgetPolicy = contextBudgetPolicy;
        _turnPreparation = turnPreparation;
        _turnCompletionRecorder = turnCompletionRecorder;
        _turnState = turnState;
    }

    /// <summary>
    /// Drops any suggestion chip that does not match a real entity name for the given recipe slot.
    /// The LLM's [SUGGESTIONS: ...] block is parsed from free text with no grounding (LLMResponseBuilder),
    /// so a plausible-sounding but non-existent contract/group name can otherwise reach the user as a
    /// toast before the (hard, DB-backed) skill resolution step ever runs. No-ops when the slot is null
    /// or not one ISuggestionEntityNameReader knows how to ground.
    /// </summary>
    internal async Task ApplySuggestionGroundingAsync(LLMResponse response, string? slot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slot) || response.Suggestions is not { Count: > 0 })
        {
            return;
        }

        var realNames = await _suggestionEntityNameReader.GetRealNamesForSlotAsync(slot, cancellationToken);
        if (realNames == null)
        {
            return;
        }

        response.Suggestions = SuggestionGroundingFilter.Filter(response.Suggestions, realNames);
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId}: {Message}",
                context.UserId, context.Message);

            var (model, provider, error, conversation, systemPrompt, volatilePrompt, truncatedHistory, budgetProfile) =
                await PrepareContextAsync(context);

            if (error != null) return _responseBuilder.BuildErrorResponse(error);

            if (context.CorrectionClarificationReply is { Length: > 0 } clarification)
            {
                try
                {
                    await PersistClarificationTurnAsync(
                        context, conversation!, model!, provider!, clarification, stopwatch, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving the correction clarification for user {UserId}", context.UserId);
                }

                return BuildClarificationResponse(conversation!, clarification);
            }

            var totalUsage = new Providers.LLMUsage();
            var ctx = new MultiTurnContext(
                context, model!, provider!, systemPrompt!, truncatedHistory!, totalUsage, conversation!, stopwatch,
                volatilePrompt ?? string.Empty, budgetProfile, cancellationToken);

            var (responseContent, lastResponse, iterationsUsed, allFunctionCalls, askedSlot) =
                await ExecuteMultiTurnLoopAsync(ctx);

            if (lastResponse is { Success: false })
            {
                return _responseBuilder.BuildErrorResponse(lastResponse.Error ?? "An error occurred.");
            }

            await _conversationManager.SaveConversationMessagesAsync(
                conversation!, context.Message, responseContent, model!.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation!,
                totalUsage, stopwatch.ElapsedMilliseconds,
                toolsetAssemblyMs: context.ToolsetAssemblyMs, toolIterations: iterationsUsed,
                turnId: context.TurnId, functionsCalledJson: SerializeFunctionsCalled(allFunctionCalls),
                toolChoiceRequested: ctx.ToolChoiceRequested,
                toolChoiceSupported: ctx.Provider.SupportsToolChoice,
                toolCallReturned: allFunctionCalls.Count > 0);

            _turnPreparation.RecordLastAction(
                context, conversation!.ConversationId, responseContent, allFunctionCalls, ctx.RecipePausedOnAsk);

            var agent = await _agentRepository.GetDefaultAgentAsync();
            _backgroundTaskService.RunBackgroundTasks(agent, conversation!, context, responseContent, allFunctionCalls, ctx.AnsweredWithNotice);

            var response = _responseBuilder.BuildSuccessResponse(
                lastResponse!, conversation!.ConversationId, responseContent, allFunctionCalls,
                _functionExecutor.NavigationRoute, _functionExecutor.NavigationTarget);
            await ApplySuggestionGroundingAsync(response, askedSlot);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request for user {UserId}", context.UserId);
            return _responseBuilder.BuildErrorResponse("An internal error occurred.");
        }
    }

    public async IAsyncEnumerable<SseChunk> ProcessStreamAsync(
        LLMContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _turnState.Begin(context);

        yield return SseChunk.Status(SseStatusStages.PreparingContext, ElapsedMsFor(context));

        string? preparationError = null;
        (LLMModel? model, ILLMProvider? provider, string? error,
            LLMConversation? conversation, string? systemPrompt, string? volatilePrompt,
            List<Providers.LLMMessage>? truncatedHistory, ContextBudgetProfile? budgetProfile) prepared = default;

        try
        {
            prepared = await PrepareContextAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing stream context for user {UserId}", context.UserId);
            preparationError = AssistantStreamErrorMessages.ContextPreparationFailure;
        }

        if (preparationError != null)
        {
            _turnState.TrySetOutcome(TurnOutcome.Errored);
            yield return SseChunk.Error(preparationError);
            yield break;
        }

        var (model, provider, prepError, conversation, systemPrompt, volatilePrompt, history, budgetProfile) = prepared;

        if (prepError != null)
        {
            _turnState.TrySetOutcome(TurnOutcome.Errored);
            yield return SseChunk.Error(prepError);
            yield break;
        }

        yield return SseChunk.StreamStart(conversation!.ConversationId, context.TurnId);

        if (context.CorrectionClarificationReply is { Length: > 0 } streamClarification)
        {
            yield return SseChunk.Content(streamClarification);
            _turnState.TrySetOutcome(TurnOutcome.Clarified);

            try
            {
                await PersistClarificationTurnAsync(
                    context, conversation!, model!, provider!, streamClarification, stopwatch, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving the correction clarification for user {UserId}", context.UserId);
            }

            yield return SseChunk.Metadata(BuildClarificationResponse(conversation!, streamClarification));
            yield return SseChunk.Done();
            yield break;
        }

        var turn = _turnState;
        turn.Attach(conversation!, model!, provider!.SupportsToolChoice);
        var runningHistory = new List<Providers.LLMMessage>(history!);
        var currentMessage = context.Message;
        var historyBudget = HistoryBudgetFor(provider!, model!, systemPrompt, volatilePrompt, context.AvailableFunctions);
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(context.Message);

        if (turn.StopRequested)
        {
            foreach (var stoppedChunk in StoppedTurnChunks(turn))
            {
                yield return stoppedChunk;
            }

            yield break;
        }

        // Emitted unconditionally, not only when a recipe turns out to be active: the resolve itself
        // runs on every turn (recipe table read, trigger matching, semantic fallback and slot
        // extraction — the last of which is a model call), so the wait is real regardless of outcome.
        yield return SseChunk.Status(SseStatusStages.ResolvingRecipe, ElapsedMsFor(context));

        var recipe = await RecipeTurnState.BeginAsync(
            _turnPreparation, _recipeRunRecorder, _recipeEngine, _logger,
            context, provider!, model!, conversation!.ConversationId, cancellationToken);
        var lastCallStart = 0;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            if (turn.StopRequested)
            {
                break;
            }

            turn.ToolIterations = iteration + 1;
            lastCallStart = turn.StreamedContent.Length;
            FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);

            await foreach (var stepChunk in RecipePauseStep.StreamAsync(
                               _logger, recipe, provider!, model!, currentMessage, systemPrompt!, volatilePrompt,
                               runningHistory, turn, cancellationToken))
            {
                yield return stepChunk;
            }

            if (recipe.PausedOnAsk || turn.StopRequested)
            {
                break;
            }

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

            var (forceRecipe, recipeFunctions, recipeNote) = ResolveRecipeIteration(
                recipe.Forcing, confirmThisIteration, context.AvailableFunctions, iterationFunctions);
            iterationFunctions = recipeFunctions;
            if (forceRecipe)
            {
                _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                    recipe.Forcing!.Name, recipe.Forcing.CurrentSkill, iteration);
            }

            var providerRequest = LLMProviderRequestFactory.ForIteration(
                model!, currentMessage, systemPrompt!,
                CombineVolatile(volatilePrompt, IterationNotePolicy.Select(
                    confirmThisIteration, recipe.PendingNote, forceRecipe, recipeNote,
                    recipe.SuggestPlan, turn.Calls.Count)),
                runningHistory, iterationFunctions,
                ToolChoicePolicy.ResolveToolChoice(
                    forceRecipe, isMutationIntent, isNavigationIntent, recipe.ForceConfirm, turn.Calls.Count),
                stream: true,
                onStreamUsage: usage => LLMUsageAccumulator.Add(turn.Usage, usage));

            if (providerRequest.ToolChoice == MutationGuardConstants.ToolChoiceRequired)
            {
                turn.ToolChoiceRequested = true;
            }

            yield return SseChunk.Status(SseStatusStages.CallingModel, ElapsedMsFor(context), turn.ToolIterations);

            var modelCall = new StreamedModelCall(_logger);
            await foreach (var callChunk in modelCall.RunAsync(
                               provider!, providerRequest, model!, turn, stopwatch, cancellationToken))
            {
                yield return callChunk;
            }

            if (modelCall.Error != null)
            {
                turn.TrySetOutcome(TurnOutcome.Errored);
                yield return SseChunk.Error(modelCall.Error);
                yield break;
            }

            var accumulator = modelCall.Accumulator;

            if (modelCall.Cancelled && !turn.StopRequested)
            {
                yield break;
            }

            if (turn.StopRequested)
            {
                break;
            }

            if (!accumulator.HasFunctionCalls)
                break;

            var functionCalls = accumulator.FunctionCalls.ToList();
            turn.RegisterCalls(functionCalls);
            ApplyRecipeInjections(recipe.Forcing, functionCalls);

            var executableCalls = RepeatedWriteCallGuard.RejectAndRecord(functionCalls, calledFunctionNames, forceRecipe);

            var toolRound = new StreamedToolRound(_functionExecutor);
            await foreach (var roundChunk in toolRound.RunAsync(turn, recipe, functionCalls, executableCalls))
            {
                yield return roundChunk;
            }

            if (toolRound.EndsTurn || turn.StopRequested)
                break;

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            runningHistory.Add(new Providers.LLMMessage
                { Role = "assistant", Content = AnswerPlaceholder.ForToolCallTurn(accumulator.AccumulatedContent, functionCalls) });
            currentMessage = FormatFunctionResults(functionCalls, budgetProfile?.MaxToolResultChars)
                + recipe.TakeGateHoldNote();
        }

        var closing = new StreamedTurnClosing(_logger, _functionExecutor);
        await foreach (var closingChunk in closing.StreamAsync(
                           turn,
                           recipe,
                           new TurnClosingInput(
                               provider!, model!, currentMessage, systemPrompt!, volatilePrompt, runningHistory,
                               historyBudget, lastCallStart, isMutationIntent),
                           cancellationToken))
        {
            yield return closingChunk;
        }

        if (turn.StopRequested)
        {
            foreach (var stoppedChunk in StoppedTurnChunks(turn))
            {
                yield return stoppedChunk;
            }

            yield break;
        }

        var responseContent = turn.StreamedContent.ToString();

        await _turnCompletionRecorder.RecordCompletedAsync(
            new TurnCompletion(
                context, conversation!, model!, provider!.SupportsToolChoice, responseContent, turn.Usage,
                stopwatch.ElapsedMilliseconds, turn.TtftMs, turn.ToolIterations, turn.Calls,
                turn.ToolChoiceRequested, recipe.PausedOnAsk, turn.AnsweredWithNotice),
            CancellationToken.None);

        var metadataResponse = _responseBuilder.BuildSuccessResponse(
            new LLMProviderResponse { Content = responseContent, Usage = turn.Usage, Success = true },
            conversation!.ConversationId, responseContent, turn.Calls, turn.NavigationRoute, turn.NavigationTarget);
        await ApplySuggestionGroundingAsync(metadataResponse, recipe.AskedSlot, cancellationToken);

        yield return SseChunk.Metadata(metadataResponse);
        yield return SseChunk.Done();
    }

    /// <summary>
    /// The closing events of a turn the user stopped: turn_stopped and then done, nothing else, because the
    /// client keeps processing the stream after a confirmed stop and would otherwise append text or start UI
    /// actions to a stopped turn. Claims the Stopped outcome first, so the safety net leaves the turn alone.
    /// </summary>
    /// <param name="turn">The stopped turn</param>
    private static IEnumerable<SseChunk> StoppedTurnChunks(TurnRunState turn)
    {
        turn.TrySetOutcome(TurnOutcome.Stopped);
        var executedCount = turn.Calls.Count(c => c.Success && !c.RequiresConfirmation && !c.IsRejectedRepeat && !c.SkippedByStop);
        yield return SseChunk.TurnStopped(turn.Context!.TurnId.GetValueOrDefault(), new List<string>(), executedCount);
        yield return SseChunk.Done();
    }

    /// <summary>
    /// The save/track/background tail of a turn that answered with the authored clarification question of
    /// a graceful correction instead of calling the model. Shared by both chat paths so the streaming and
    /// the non-streaming chat persist exactly the same turn. Usage is tracked with an empty usage record
    /// because no provider was called at all, and the background tasks run with an empty call list, so the
    /// turn is captured as what it was: a correction answered with a question and no action.
    /// RecordLastAction is deliberately NOT called: this turn executed nothing, so there is nothing to
    /// record, and calling it would only redundantly mark the record superseded. The pins the entry point
    /// wrote onto that record are not at risk either way - a superseded record still carries them.
    /// </summary>
    private async Task PersistClarificationTurnAsync(
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
    private LLMResponse BuildClarificationResponse(LLMConversation conversation, string clarification) =>
        _responseBuilder.BuildSuccessResponse(
            new LLMProviderResponse { Content = clarification, Usage = new Providers.LLMUsage(), Success = true },
            conversation.ConversationId, clarification, new List<LLMFunctionCall>(), null, null);

    /// <summary>
    /// Calls the provider and retries on transient failures (rate limit, overload, gateway errors)
    /// with linear backoff. Non-transient errors and exhausted retries return the failed response as-is.
    /// </summary>
    /// <param name="provider">The LLM provider to call</param>
    /// <param name="request">The provider request to (re-)send</param>
    /// <param name="cancellationToken">Cancels the backoff delay between attempts</param>
    internal Task<LLMProviderResponse> ProcessWithTransientRetryAsync(
        ILLMProvider provider, LLMProviderRequest request, CancellationToken cancellationToken = default) =>
        TransientProviderRetry.ProcessAsync(provider, request, _logger, cancellationToken);

    private async Task<(LLMModel? model, ILLMProvider? provider, string? error,
        LLMConversation? conversation, string? systemPrompt, string? volatilePrompt,
        List<Providers.LLMMessage>? history, ContextBudgetProfile? budgetProfile)>
        PrepareContextAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var stageWatch = Stopwatch.StartNew();

        var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(context.ModelId);
        if (error != null) return (null, null, error, null, null, null, null, null);

        var budgetProfile = _contextBudgetPolicy.Resolve(provider!, model!);

        var conversation = await _conversationManager.GetOrCreateConversationAsync(context.ConversationId, context.UserId);
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);

        stageWatch.Restart();
        var llmHistory = await _conversationManager.GetConversationHistoryAsync(conversation.ConversationId, conversation.UserId);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "GetConversationHistory", stageWatch.ElapsedMilliseconds);

        SoulAndMemoryPrompt? soulAndMemoryPrompt = null;
        if (agent != null)
        {
            stageWatch.Restart();
            var availableSkillNames = context.AvailableFunctions?.Select(f => f.Name).ToList();
            Guid? userId = Guid.TryParse(context.UserId, out var parsedUserId) ? parsedUserId : null;
            soulAndMemoryPrompt = await _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(
                agent.Id, context.Message, context.Language, availableSkillNames, context.ScopedClientPolicy,
                hasDomainSkillContext: context.HasDomainSkillContext ?? true,
                userId: userId,
                conversationId: context.ConversationId,
                pageContext: context.PageContext,
                isVoiceMode: context.IsVoiceMode,
                budgetProfile: budgetProfile);
            if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
                _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "AssembleSoulAndMemory", stageWatch.ElapsedMilliseconds);
        }

        context.InjectedMemoryIds = soulAndMemoryPrompt?.InjectedMemoryIds;

        stageWatch.Restart();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt?.StablePrompt);
        var temporalContext = await _promptBuilder.BuildTemporalContextAsync(context, cancellationToken);
        var volatilePrompt = BuildVolatilePrompt(temporalContext, context, soulAndMemoryPrompt?.VolatilePrompt);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "BuildSystemPrompt", stageWatch.ElapsedMilliseconds);

        if (context.IsVoiceMode)
        {
            _logger.LogInformation(
                "Voice turn: spoken-answer directive appended={Appended}, prompt {Length} chars",
                systemPrompt.Contains(VoiceModeInstructionConstants.SpokenAnswerDirective),
                systemPrompt.Length);
        }

        var historyBudget = HistoryBudgetFor(provider!, model!, systemPrompt, volatilePrompt, context.AvailableFunctions);
        var truncatedHistory = TruncateHistory(llmHistory, historyBudget, conversation.Summary, budgetProfile.MaxHistoryMessages);

        return (model, provider, null, conversation, systemPrompt, volatilePrompt, truncatedHistory, budgetProfile);
    }

    /// <summary>
    /// The turn's volatile system-prompt segment: temporal context, the context-derived additions, the
    /// soul/memory segment and - on a correction turn - LLMContext.CorrectionNote. The correction note
    /// belongs HERE rather than in the per-iteration note slot of the loop: the pending/recipe/plan notes
    /// are alternatives to each other, while a correction note applies to every iteration of the turn it
    /// belongs to. Volatile rather than stable so a note that changes every turn cannot invalidate a
    /// provider's cached stable segment.
    /// </summary>
    /// <param name="temporalContext">The turn's date/time block.</param>
    /// <param name="context">The turn context, source of the volatile additions and the correction note.</param>
    /// <param name="soulAndMemoryVolatilePrompt">Volatile half of the soul/memory assembly, null when it did not run.</param>
    internal static string BuildVolatilePrompt(
        string? temporalContext, LLMContext context, string? soulAndMemoryVolatilePrompt) =>
        CombineVolatile(
            temporalContext,
            CombineVolatile(
                CombineVolatile(LLMSystemPromptBuilder.BuildVolatileAdditions(context), soulAndMemoryVolatilePrompt),
                context.CorrectionNote));

    // Milliseconds since the turn clock started, i.e. before the toolset assembly the caller already
    // paid for. Null when no clock was handed in, which keeps elapsedMs off the wire instead of
    // reporting an age measured from an unrelated zero point.
    internal static long? ElapsedMsFor(LLMContext context) =>
        context.TurnStartTimestamp is { } start
            ? (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds
            : null;

    // The turn's short correlation id: the same value the ambient TurnCorrelation carries into the
    // retrieval log, so both sides of a turn can be joined without threading an id through the layers.
    internal static string TurnCorrelationFor(LLMContext context) =>
        context.TurnId is { } turnId ? TurnCorrelation.Format(turnId) : TurnCorrelation.CurrentOrNone;

    // Effective per-turn budget for conversation history, derived from the provider's real input limit
    // for this model. Shared by the initial truncation and the in-loop re-truncation so both use the
    // exact same ceiling. Sums the stable and volatile system-prompt segments so the budget reflects
    // the full prompt actually sent to the provider, regardless of how it is split into cache blocks.
    // availableFunctions is the toolset SkillToolsetAssembler already assembled for this turn (final
    // by the time budgeting runs in both call sites), so the tool-definition reserve reflects what is
    // actually sent instead of a pessimistic flat constant.
    // The volatile segment also carries LLMContext.CorrectionNote on a correction turn, so such a turn
    // budgets roughly 120 tokens less history than an ordinary one. That is deliberate:
    // MinHistoryBudgetTokens stays the floor and the note lives exactly one turn.
    private static int HistoryBudgetFor(
        ILLMProvider provider, LLMModel model, string? systemPrompt, string? volatileSystemPrompt, List<LLMFunction>? availableFunctions) =>
        ComputeHistoryBudget(
            provider.GetEffectiveInputTokenLimit(model),
            model.MaxTokens,
            EstimateTokens(systemPrompt) + EstimateTokens(volatileSystemPrompt),
            EstimateToolDefinitionReserveTokens(availableFunctions));

    internal async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls, string? askedSlot)> ExecuteMultiTurnLoopAsync(
        MultiTurnContext ctx)
    {
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var allFunctionCalls = new List<LLMFunctionCall>();
        var runningHistory = new List<Providers.LLMMessage>(ctx.TruncatedHistory);
        var currentMessage = ctx.Context.Message;
        var historyBudget = HistoryBudgetFor(ctx.Provider, ctx.Model, ctx.SystemPrompt, ctx.VolatilePrompt, ctx.Context.AvailableFunctions);
        string responseContent = "";
        LLMProviderResponse? lastResponse = null;
        int iterationsUsed = 0;
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(ctx.Context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(ctx.Context.Message);
        // RecipeTurnState.BeginAsync writes the plan's name onto the shared context object rather than
        // returning it: ProcessAsync holds the very same LLMContext instance and hands it to the post-turn
        // hooks, so the name reaches trajectory capture without widening this method's already six-wide
        // return tuple.
        var recipe = await RecipeTurnState.BeginAsync(
            _turnPreparation, _recipeRunRecorder, _recipeEngine, _logger,
            ctx.Context, ctx.Provider, ctx.Model, ctx.Conversation.ConversationId, ctx.CancellationToken);
        var enginePlan = recipe.Plan;
        var forcedRetryUsed = false;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;

            FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);

            enginePlan?.AdvanceOverSatisfied();
            if (enginePlan != null && enginePlan.NeedsConfirmation)
            {
                var confirmInstruction = enginePlan.ConfirmationInstruction;
                var confirmRequest = LLMProviderRequestFactory.RecipeStep(
                    ctx.Model, currentMessage, ctx.SystemPrompt, ctx.VolatilePrompt, confirmInstruction, runningHistory);

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, confirmRequest, ctx.CancellationToken);
                LLMUsageAccumulator.Add(ctx.TotalUsage, lastResponse.Usage);
                if (lastResponse.Success)
                {
                    responseContent = RecipeReplyGuard.WithConfirmationChip(RecipeReplyGuard.SafeConfirmation(
                        lastResponse.Content, enginePlan.Goal, enginePlan.AlternativeGoal, ctx.Context.Language,
                        enginePlan.GoalTranslations, enginePlan.AlternativeGoalTranslations), enginePlan.AlternativeGoal, ctx.Context.Language);
                }

                await recipe.PauseForConfirmationAsync(ctx.CancellationToken);
                break;
            }

            if (enginePlan != null && enginePlan.IsActive && enginePlan.CurrentIsAsk && !enginePlan.TopicSwitchThisTurn)
            {
                var askInstruction = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    RecipeEngineDefaults.AskStepInstructionTemplate, enginePlan.CurrentAskPrompt);
                var askRequest = LLMProviderRequestFactory.RecipeStep(
                    ctx.Model, currentMessage, ctx.SystemPrompt, ctx.VolatilePrompt, askInstruction, runningHistory);

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, askRequest, ctx.CancellationToken);
                LLMUsageAccumulator.Add(ctx.TotalUsage, lastResponse.Usage);
                if (lastResponse.Success)
                {
                    responseContent = RecipeReplyGuard.SafeAsk(
                        lastResponse.Content, enginePlan.CurrentAskPrompt ?? string.Empty,
                        enginePlan.CurrentAskPromptTranslations, ctx.Context.Language);
                }

                await recipe.PauseOnAskAsync(ctx.CancellationToken);
                break;
            }

            // Same stability rule as the streaming loop: the toolset never changes across iterations
            // (prompt-prefix cache), repeats of write skills are rejected at execution time instead.
            var iterationFunctions = ctx.Context.AvailableFunctions;

            var confirmThisIteration = recipe.ForceConfirm && allFunctionCalls.Count == 0;
            if (confirmThisIteration)
            {
                iterationFunctions = new List<LLMFunction> { recipe.ConfirmFunction! };
            }

            var (forceRecipe, recipeFunctions, recipeNote) = ResolveRecipeIteration(
                recipe.Forcing, confirmThisIteration, ctx.Context.AvailableFunctions, iterationFunctions);
            iterationFunctions = recipeFunctions;
            if (forceRecipe)
            {
                _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                    recipe.Forcing!.Name, recipe.Forcing.CurrentSkill, iteration);
            }

            var providerRequest = LLMProviderRequestFactory.ForIteration(
                ctx.Model, currentMessage, ctx.SystemPrompt,
                CombineVolatile(ctx.VolatilePrompt, IterationNotePolicy.Select(
                    confirmThisIteration, recipe.PendingNote, forceRecipe, recipeNote,
                    recipe.SuggestPlan, allFunctionCalls.Count)),
                runningHistory, iterationFunctions,
                ToolChoicePolicy.ResolveToolChoice(
                    forceRecipe, isMutationIntent, isNavigationIntent, recipe.ForceConfirm, allFunctionCalls.Count));

            if (providerRequest.ToolChoice == MutationGuardConstants.ToolChoiceRequired)
            {
                ctx.ToolChoiceRequested = true;
            }

            lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, providerRequest, ctx.CancellationToken);
            LLMUsageAccumulator.Add(ctx.TotalUsage, lastResponse.Usage);

            if (!lastResponse.Success)
            {
                _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
                    iterationsUsed, lastResponse.Error);
                await _conversationManager.TrackUsageAsync(
                    ctx.Context.UserId, ctx.Model, ctx.Conversation,
                    ctx.TotalUsage, ctx.Stopwatch.ElapsedMilliseconds,
                    hasError: true, errorMessage: lastResponse.Error,
                    toolsetAssemblyMs: ctx.Context.ToolsetAssemblyMs, toolIterations: iterationsUsed,
                    turnId: ctx.Context.TurnId, functionsCalledJson: SerializeFunctionsCalled(allFunctionCalls),
                    toolChoiceRequested: ctx.ToolChoiceRequested,
                    toolChoiceSupported: ctx.Provider.SupportsToolChoice,
                    toolCallReturned: allFunctionCalls.Count > 0);
                return (lastResponse.Error ?? "An error occurred.", lastResponse, iterationsUsed, allFunctionCalls, null);
            }

            responseContent = lastResponse.Content;

            if (!lastResponse.FunctionCalls.Any())
            {
                // V1 (non-streaming): nothing is sent to the client until the loop ends, so a false
                // success claim can still be suppressed. If a mutation request produced no tool call,
                // retry ONCE with a forcing nudge (the next request sets tool_choice="required" because
                // allFunctionCalls is still empty) before giving up. Also trigger when intent detection
                // missed the phrasing but the model emitted a text tool-call itself (never executes).
                if (ForceToolNudgePolicy.ShouldForceToolNudge(
                        isMutationIntent, recipe.ForceConfirm,
                        ToolCallMarkupSanitizer.ContainsMarkup(lastResponse.Content),
                        CompletionClaimDetector.ClaimsCompletion(lastResponse.Content),
                        allFunctionCalls.Count, recipe.PausedOnAsk, ClarifyingResponse.IsClarifying(lastResponse.Content))
                    && !forcedRetryUsed
                    && iteration < maxIterations - 1)
                {
                    forcedRetryUsed = true;
                    runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
                    runningHistory.Add(new Providers.LLMMessage
                    {
                        Role = "assistant",
                        Content = string.IsNullOrWhiteSpace(lastResponse.Content)
                            ? LLMLoopConstants.NoActionHistoryNote
                            : lastResponse.Content
                    });
                    currentMessage = MutationGuardConstants.ForceToolNudge;
                    continue;
                }

                break;
            }

            _logger.LogInformation("Multi-turn iteration {Iteration}: executing {Count} function calls",
                iterationsUsed, lastResponse.FunctionCalls.Count);

            allFunctionCalls.AddRange(lastResponse.FunctionCalls);
            ApplyRecipeInjections(recipe.Forcing, lastResponse.FunctionCalls);

            var executableCalls = RepeatedWriteCallGuard.RejectAndRecord(
                lastResponse.FunctionCalls, calledFunctionNames, forceRecipe);

            await _functionExecutor.ProcessFunctionCallsAsync(ctx.Context, executableCalls);
            recipe.Forcing?.Observe(lastResponse.FunctionCalls);
            if (lastResponse.FunctionCalls.Any(c => c.RequiresConfirmation))
            {
                recipe.ReleaseOnAutonomyGateHold();
            }

            if (executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls)
            {
                _logger.LogInformation("All function calls are UiPassthrough - breaking multi-turn loop");
                break;
            }

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            runningHistory.Add(new Providers.LLMMessage
                { Role = "assistant", Content = AnswerPlaceholder.ForToolCallTurn(lastResponse.Content, lastResponse.FunctionCalls) });
            currentMessage = FormatFunctionResults(lastResponse.FunctionCalls, ctx.BudgetProfile?.MaxToolResultChars)
                + recipe.TakeGateHoldNote();
        }

        responseContent = await RecoverAnswerAsync(
            ctx, currentMessage, runningHistory, historyBudget, responseContent, lastResponse, allFunctionCalls, recipe.PausedOnAsk, forcedRetryUsed);

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
                "All function calls failed in multi-turn loop; surfacing notice for {FunctionName}. Raw result: {RawResult}",
                failedCall.FunctionName, failedCall.Result);
            responseContent = TurnClosingNotices.StepFailed(failedCall);
        }

        if (allFunctionCalls.Count > 0)
        {
            _logger.LogInformation("Multi-turn completed: {TotalCalls} function calls in {Iterations} iterations",
                allFunctionCalls.Count, iterationsUsed);
        }

        ctx.RecipePausedOnAsk = recipe.PausedOnAsk;

        return (responseContent, lastResponse, iterationsUsed, allFunctionCalls, recipe.AskedSlot);
    }

    // Prompt-injection containment lives in ToolResultFormatter, shared with the turn replay and the
    // read-only research sub-loop. Internal (not private): covered by LLMServiceFormatFunctionResultsTests.
    internal static string FormatFunctionResults(List<LLMFunctionCall> functionCalls, int? maxToolResultChars = null) =>
        ToolResultFormatter.Format(functionCalls, maxToolResultChars ?? LLMLoopConstants.DefaultMaxToolResultChars);

    // Recipe forcing spine (shared by both the streaming and non-streaming loops so a hook can never
    // land on only one path): while a recipe plan is active and a confirmation is not already being
    // forced, narrow the iteration's tool scope to the recipe's current step skill and report that the
    // step is being forced (the caller sets tool_choice=required and appends the step note). This forces
    // the ordered chain step by step, not just a single skill.
    private static (bool Forcing, List<LLMFunction> Functions, string? StepNote) ResolveRecipeIteration(
        IRecipeForcingPlan? recipePlan,
        bool confirmThisIteration,
        List<LLMFunction> availableFunctions,
        List<LLMFunction> iterationFunctions)
    {
        if (confirmThisIteration || recipePlan?.IsActive != true)
        {
            return (false, iterationFunctions, null);
        }

        var recipeFunction = availableFunctions.FirstOrDefault(
            f => string.Equals(f.Name, recipePlan.CurrentSkill, StringComparison.OrdinalIgnoreCase));
        if (recipeFunction == null)
        {
            return (false, iterationFunctions, null);
        }

        return (true, new List<LLMFunction> { recipeFunction }, recipePlan.CurrentStepNote);
    }

    // Recipe forcing data flow (shared by both loops): deterministically inject captured values (the
    // resolved clientId from find_customer_candidates) into the next forced step's parameters before it
    // executes — reliable in-code data flow between steps, not a fragile model-carries-the-id hop.
    private static void ApplyRecipeInjections(IRecipeForcingPlan? recipePlan, IEnumerable<LLMFunctionCall> functionCalls)
    {
        if (recipePlan == null)
        {
            return;
        }

        foreach (var call in functionCalls)
        {
            foreach (var injection in recipePlan.GetParameterInjections(call.FunctionName))
            {
                call.Parameters[injection.Key] = injection.Value;
            }
        }
    }

    /// <summary>
    /// Non-streaming side of the closing guard. A failed last response (a recipe confirmation or ask call)
    /// turns the whole turn into an error response, so recovering its answer would be a wasted call. A turn
    /// without tool calls recovers against the user's message, never against a force-tool nudge, and
    /// without the nudge's exchange, which would otherwise repeat that message in the history.
    /// </summary>
    private async Task<string> RecoverAnswerAsync(
        MultiTurnContext ctx, string currentMessage, List<Providers.LLMMessage> runningHistory, int historyBudget,
        string responseContent, LLMProviderResponse? lastResponse, List<LLMFunctionCall> allFunctionCalls, bool pausedOnRecipeStep,
        bool nudged)
    {
        if (lastResponse is { Success: false })
        {
            return responseContent;
        }

        var noToolRan = allFunctionCalls.Count == 0;
        var recovery = StreamedTurnClosing.RecoveryFor(
            _logger, ctx.Provider, ctx.TotalUsage, ctx.Model, noToolRan ? ctx.Context.Message : currentMessage, ctx.SystemPrompt, ctx.VolatilePrompt,
            noToolRan && nudged ? ForceToolNudgePolicy.WithoutNudgeExchange(runningHistory, ctx.Context.Message) : runningHistory,
            historyBudget, ctx.Context.Language);
        var answer = await recovery.ResolveAsync(
            responseContent, allFunctionCalls, () => _functionExecutor.LastBatchWasUiPassthroughOnly, pausedOnRecipeStep, ctx.CancellationToken);
        ctx.AnsweredWithNotice = recovery.AnsweredWithNotice;
        return answer;
    }

    private static int EstimateTokens(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : text.Length / CharsPerToken;

    private static int EstimateTokens(IEnumerable<Providers.LLMMessage> messages) =>
        messages.Sum(m => EstimateTokens(m.Content));

    // The token budget available for conversation history (and, in the multi-turn loop, the growing
    // running history + function-result message). Derived from the provider-reported EFFECTIVE input
    // limit for the model so it adapts automatically per model instead of trusting a possibly-inflated
    // nominal context window. Output room, the measured native tool-definition size and a fixed
    // interaction headroom are reserved so the context is never filled to the brim. Never goes below
    // MinHistoryBudgetTokens, even when every other reservation is pessimistic.
    internal static int ComputeHistoryBudget(int effectiveInputLimit, int maxOutputTokens, int systemPromptTokens, int toolDefinitionTokens)
    {
        var budget = effectiveInputLimit
            - maxOutputTokens
            - systemPromptTokens
            - toolDefinitionTokens
            - InteractionHeadroomTokens
            - SafetyMarginTokens;

        return Math.Max(budget, MinHistoryBudgetTokens);
    }

    // Real per-turn reserve for the native tool/function definitions attached to the request, measured
    // from the toolset SkillToolsetAssembler actually assembled for this turn instead of a fixed
    // pessimistic constant. Null or empty toolsets correctly reserve 0 tokens - if no tools are sent to
    // the provider, there is nothing to budget for, and the full input limit remains available to
    // history.
    internal static int EstimateToolDefinitionReserveTokens(List<LLMFunction>? functions)
    {
        if (functions == null || functions.Count == 0)
            return 0;

        var rawTokens = functions.Sum(EstimateToolDefinitionTokens);
        return rawTokens + (rawTokens * ToolDefinitionSafetyMarginPercent / 100);
    }

    // Character-based JSON size of a single tool definition, mirroring the
    // {name, description, parameters:{type, properties, required}} shape every supported provider
    // serializes a function/tool into (Anthropic input_schema, OpenAI/DeepSeek/Groq/Gemini parameters
    // schema) so the estimate is provider-neutral rather than tuned to one API's wire format.
    private static int EstimateToolDefinitionTokens(LLMFunction function)
    {
        var json = JsonSerializer.Serialize(new
        {
            name = function.Name,
            description = function.Description,
            parameters = new
            {
                type = "object",
                properties = function.Parameters,
                required = function.RequiredParameters
            }
        });

        return EstimateTokens(json);
    }

    internal static List<Providers.LLMMessage> TruncateHistory(
        List<Providers.LLMMessage> history,
        int historyBudgetTokens,
        string? conversationSummary = null,
        int maxHistoryMessages = MaxHistoryMessages)
    {
        var summaryContent = ConversationSummaryCodec.RenderInner(conversationSummary);
        var hasSummary = summaryContent != null;

        var historyBudget = historyBudgetTokens;
        if (hasSummary)
        {
            historyBudget -= EstimateTokens(summaryContent) + SummaryBudgetReserveTokens;
        }

        if (history.Count <= maxHistoryMessages && !hasSummary && EstimateTokens(history) <= historyBudget)
            return history;

        var truncated = new List<Providers.LLMMessage>();
        var tokenCount = 0;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(history[i].Content);
            tokenCount += msgTokens;

            if (tokenCount > historyBudget || truncated.Count >= maxHistoryMessages)
                break;

            truncated.Insert(0, history[i]);
        }

        if (hasSummary)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = "system",
                Content = $"[Conversation Summary (earlier messages)]\n{summaryContent}\n[/Conversation Summary]"
            });
        }
        else if (truncated.Count < history.Count)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = "system",
                Content = TruncationNotice
            });
        }

        return truncated;
    }

    // Called before every provider call inside the multi-turn tool loop. The running history grows by a
    // user + assistant message each iteration and the function-result message can be large, so without
    // this the accumulated prompt can exceed the model's input limit mid-loop. Drops the oldest
    // non-system messages (a leading conversation-summary system message is preserved) until the
    // estimated running history plus the next message fits the same budget used for the initial history.
    internal static void FitRunningHistoryToBudget(
        List<Providers.LLMMessage> runningHistory,
        string nextMessage,
        int historyBudgetTokens)
    {
        var total = EstimateTokens(nextMessage) + EstimateTokens(runningHistory);

        while (total > historyBudgetTokens && runningHistory.Count > 0)
        {
            var dropIndex = runningHistory[0].Role == "system" ? 1 : 0;
            if (dropIndex >= runningHistory.Count)
                break;

            total -= EstimateTokens(runningHistory[dropIndex].Content);
            runningHistory.RemoveAt(dropIndex);
        }
    }

    // W1.7: fills llm_usage.functions_called with the distinct function names this turn actually
    // invoked, as a JSON array capped to the varchar(200) column. Truncation keeps the row writable;
    // the column is a plain string, so a truncated tail is acceptable for SQL analysis.
    internal static string SerializeFunctionsCalled(IReadOnlyList<LLMFunctionCall> allFunctionCalls)
    {
        var names = allFunctionCalls
            .Select(c => c.FunctionName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return "[]";
        }

        const int maxLength = 200;
        var json = JsonSerializer.Serialize(names);
        if (json.Length <= maxLength)
        {
            return json;
        }

        while (names.Count > 1 && JsonSerializer.Serialize(names).Length > maxLength)
        {
            names.RemoveAt(names.Count - 1);
        }

        json = JsonSerializer.Serialize(names);
        return json.Length <= maxLength ? json : json[..maxLength];
    }

    // Appends a per-turn instruction note (confirmation gate, ask-step, forced recipe, plan nudge) to the
    // volatile system-prompt segment instead of the stable one, so a note that changes every turn can
    // never invalidate a provider's cached stable segment (e.g. Anthropic prompt caching).
    internal static string CombineVolatile(string? basePrompt, string? note)
    {
        if (string.IsNullOrEmpty(note))
        {
            return basePrompt ?? string.Empty;
        }

        return string.IsNullOrEmpty(basePrompt) ? note : $"{basePrompt}\n\n{note}";
    }
}
