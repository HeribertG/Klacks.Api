// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
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
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly RecipeEngineService _recipeEngine;
    private readonly RecipeSlotExtractor _slotExtractor;
    private readonly ISuggestionEntityNameReader _suggestionEntityNameReader;

    private const int MaxHistoryMessages = 20;

    // Rough characters-per-token ratio used for all local prompt-size estimates. Deliberately
    // low (conservative) so estimates over- rather than under-count real tokenizer output.
    private const int CharsPerToken = 4;

    // Reserve for the native tool/function definitions attached to the request. These are NOT part
    // of the system-prompt string, so they must be budgeted separately. Covers the always-on skills
    // plus the per-turn retrieved set.
    private const int ToolDefinitionReserveTokens = 15_000;

    // Headroom deliberately kept free on every turn so that a single recall/list answer cannot fill the
    // whole context window — leaving room for the model's reply and for follow-up interactions.
    private const int InteractionHeadroomTokens = 8_000;

    // Extra slack absorbing tokenizer/estimate drift (our CharsPerToken estimate is approximate).
    private const int SafetyMarginTokens = 2_000;

    // Never starve history below this, even if overhead estimates are pessimistic.
    private const int MinHistoryBudgetTokens = 4_000;

    // Per function-result cap fed back into the loop, so one huge tool payload cannot blow the budget.
    private const int MaxToolResultChars = 8_000;

    private const int StageLogThresholdMs = 50;

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
        IPendingConfirmationStore pendingConfirmationStore,
        RecipeEngineService recipeEngine,
        RecipeSlotExtractor slotExtractor,
        ISuggestionEntityNameReader suggestionEntityNameReader)
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
        _pendingConfirmationStore = pendingConfirmationStore;
        _recipeEngine = recipeEngine;
        _slotExtractor = slotExtractor;
        _suggestionEntityNameReader = suggestionEntityNameReader;
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

    /// <summary>
    /// Decides whether the current turn should be forced to confirm an outstanding pending action.
    /// Fires only when the user message is a clear affirmation AND the user still has an un-consumed
    /// confirmation in the store AND confirm_pending_action is in scope. Returns the (always-on)
    /// confirm function to narrow the tool scope to, plus a context note that resurfaces the token
    /// (which is lost from conversation history because only user/assistant text is persisted).
    /// </summary>
    private (bool Force, LLMFunction? ConfirmFunction, string? ContextNote) ResolvePendingConfirmation(LLMContext context)
    {
        if (!AffirmationDetector.IsAffirmation(context.Message)
            || MutationIntentDetector.IsMutationIntent(context.Message)
            || !Guid.TryParse(context.UserId, out var userGuid))
        {
            return (false, null, null);
        }

        var pending = _pendingConfirmationStore.PeekLatestForUser(
            userGuid, TimeSpan.FromSeconds(AutonomyDefaults.ConfirmationForceWindowSeconds));
        if (pending == null)
        {
            return (false, null, null);
        }

        var confirmFunction = context.AvailableFunctions.FirstOrDefault(
            f => string.Equals(f.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase));
        if (confirmFunction == null)
        {
            return (false, null, null);
        }

        var note = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            MutationGuardConstants.PendingConfirmationContextTemplate,
            pending.SkillName,
            pending.Token);

        return (true, confirmFunction, note);
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId}: {Message}",
                context.UserId, context.Message);

            var (model, provider, error, conversation, systemPrompt, truncatedHistory) =
                await PrepareContextAsync(context);

            if (error != null) return _responseBuilder.BuildErrorResponse(error);

            var totalUsage = new Providers.LLMUsage();
            var ctx = new MultiTurnContext(context, model!, provider!, systemPrompt!, truncatedHistory!, totalUsage, conversation!, stopwatch);

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
                totalUsage, stopwatch.ElapsedMilliseconds);

            var agent = await _agentRepository.GetDefaultAgentAsync();
            _backgroundTaskService.RunBackgroundTasks(agent, conversation!, context, responseContent, allFunctionCalls);

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

        string? preparationError = null;
        (LLMModel? model, ILLMProvider? provider, string? error,
            LLMConversation? conversation, string? systemPrompt, List<Providers.LLMMessage>? truncatedHistory) prepared = default;

        try
        {
            prepared = await PrepareContextAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing stream context for user {UserId}", context.UserId);
            preparationError = $"Context preparation failed: {ex.Message}";
        }

        if (preparationError != null)
        {
            yield return SseChunk.Error(preparationError);
            yield break;
        }

        var (model, provider, prepError, conversation, systemPrompt, history) = prepared;

        if (prepError != null)
        {
            yield return SseChunk.Error(prepError);
            yield break;
        }

        yield return SseChunk.StreamStart(conversation!.ConversationId);

        var totalUsage = new Providers.LLMUsage();
        var allFunctionCalls = new List<LLMFunctionCall>();
        var fullResponseContent = new StringBuilder();
        var runningHistory = new List<Providers.LLMMessage>(history!);
        var currentMessage = context.Message;
        var historyBudget = HistoryBudgetFor(provider!, model!, systemPrompt);
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var firstTokenLogged = false;
        string? navigationRoute = null;
        string? navigationTarget = null;
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(context.Message);
        var (forceConfirmation, confirmFunction, pendingNote) = ResolvePendingConfirmation(context);
        var enginePlan = await ResolveOrResumeRecipeAsync(
            context, provider!, model!, conversation!.ConversationId, cancellationToken);
        var cutPlan = enginePlan == null ? RecipeForcingResolver.Resolve(context.Message) : null;
        IRecipeForcingPlan? recipePlan = (IRecipeForcingPlan?)enginePlan ?? cutPlan;
        Guid.TryParse(context.UserId, out var recipeUserGuid);
        var recipePausedOnAsk = false;
        string? askedSlot = null;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);

            enginePlan?.AdvanceOverSatisfied();
            if (enginePlan != null && enginePlan.NeedsConfirmation)
            {
                var confirmInstruction = enginePlan.ConfirmationInstruction;
                var confirmResponse = await ProcessWithTransientRetryAsync(provider!, new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = systemPrompt + "\n\n" + confirmInstruction,
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = model.MaxTokens,
                    CostPerInputToken = model.CostPerInputToken,
                    CostPerOutputToken = model.CostPerOutputToken
                });
                AccumulateUsage(totalUsage, confirmResponse.Usage);
                var confirmText = RecipeReplyGuard.SafeConfirmation(
                    confirmResponse.Success ? confirmResponse.Content : null,
                    enginePlan.Goal, enginePlan.AlternativeGoal, context.Language,
                    enginePlan.GoalTranslations, enginePlan.AlternativeGoalTranslations);
                fullResponseContent.Append(confirmText);
                yield return SseChunk.Content(confirmText);
                _recipeEngine.Persist(recipeUserGuid, conversation!.ConversationId, enginePlan);
                recipePausedOnAsk = true;
                _logger.LogInformation("Recipe '{Recipe}' paused for confirmation (semantic match)", enginePlan.Name);
                break;
            }

            if (enginePlan != null && enginePlan.IsActive && enginePlan.CurrentIsAsk)
            {
                var askInstruction = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    RecipeEngineDefaults.AskStepInstructionTemplate, enginePlan.CurrentAskPrompt);
                var askResponse = await ProcessWithTransientRetryAsync(provider!, new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = systemPrompt + "\n\n" + askInstruction,
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = model.MaxTokens,
                    CostPerInputToken = model.CostPerInputToken,
                    CostPerOutputToken = model.CostPerOutputToken
                });
                AccumulateUsage(totalUsage, askResponse.Usage);
                var askText = RecipeReplyGuard.SafeAsk(
                    askResponse.Success ? askResponse.Content : null, enginePlan.CurrentAskPrompt ?? string.Empty,
                    enginePlan.CurrentAskPromptTranslations, context.Language);
                fullResponseContent.Append(askText);
                yield return SseChunk.Content(askText);
                askedSlot = enginePlan.CurrentStep?.Slot;
                _recipeEngine.Persist(recipeUserGuid, conversation!.ConversationId, enginePlan);
                recipePausedOnAsk = true;
                _logger.LogInformation("Recipe '{Recipe}' paused on ask step (slot {Slot})",
                    enginePlan.Name, askedSlot);
                break;
            }

            var iterationFunctions = iteration == 0
                ? context.AvailableFunctions
                : GetReducedFunctions(context.AvailableFunctions, calledFunctionNames);

            var confirmThisIteration = forceConfirmation && allFunctionCalls.Count == 0;
            if (confirmThisIteration)
            {
                iterationFunctions = new List<LLMFunction> { confirmFunction! };
            }

            var (forceRecipe, recipeFunctions, recipeNote) = ResolveRecipeIteration(
                recipePlan, confirmThisIteration, context.AvailableFunctions, iterationFunctions);
            iterationFunctions = recipeFunctions;
            if (forceRecipe)
            {
                _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                    recipePlan!.Name, recipePlan.CurrentSkill, iteration);
            }

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = confirmThisIteration ? systemPrompt + "\n\n" + pendingNote
                    : forceRecipe ? systemPrompt + "\n\n" + recipeNote
                    : systemPrompt!,
                ModelId = model!.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                Stream = true,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                ToolChoice = (forceRecipe || ((isMutationIntent || isNavigationIntent || forceConfirmation) && allFunctionCalls.Count == 0))
                    ? MutationGuardConstants.ToolChoiceRequired
                    : null
            };

            var accumulator = new StreamAccumulator();
            var hasToolEnd = false;

            if (provider!.SupportsStreaming)
            {
                // Transient provider failures (rate limit, overload) typically kill the stream before
                // the first token. Retrying is only safe while nothing of THIS provider call has reached
                // the client — once content streamed, a retry would duplicate it, so the error surfaces.
                var transientAttempt = 0;

                while (true)
                {
                    accumulator = new StreamAccumulator();
                    hasToolEnd = false;
                    string? streamErrorMessage = null;
                    var contentEmitted = false;
                    var enumerator = provider.ProcessStreamAsync(providerRequest, cancellationToken).GetAsyncEnumerator(cancellationToken);

                    while (true)
                    {
                        string? token;
                        try
                        {
                            if (!await enumerator.MoveNextAsync()) break;
                            token = enumerator.Current;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Streaming provider error for model {ModelId}", model!.ApiModelId);
                            streamErrorMessage = $"Provider error: {ex.Message}";
                            break;
                        }

                        if (token.StartsWith(LLMStreamingTokens.ToolCallPrefix))
                        {
                            var toolJson = token[LLMStreamingTokens.ToolCallPrefix.Length..];
                            try
                            {
                                var toolData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(toolJson);
                                var index = toolData.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0;
                                var name = toolData.TryGetProperty("name", out var n) ? n.GetString() : null;
                                var args = toolData.TryGetProperty("arguments", out var a) ? a.GetString() : null;
                                accumulator.AppendToolCallDelta(index, name, args);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse tool-call delta from streaming token; token skipped");
                            }
                        }
                        else if (token == LLMStreamingTokens.ToolCallEnd)
                        {
                            hasToolEnd = true;
                        }
                        else
                        {
                            if (!firstTokenLogged)
                            {
                                _logger.LogInformation("LLM TTFT: {Ms}ms", stopwatch.ElapsedMilliseconds);
                                firstTokenLogged = true;
                            }
                            accumulator.AppendContent(token);
                            contentEmitted = true;
                            yield return SseChunk.Content(token);
                        }
                    }

                    await enumerator.DisposeAsync();

                    if (streamErrorMessage == null)
                    {
                        break;
                    }

                    if (!contentEmitted
                        && transientAttempt < LLMRetryConstants.MaxTransientRetries
                        && TransientProviderErrorDetector.IsTransient(streamErrorMessage))
                    {
                        transientAttempt++;
                        _logger.LogWarning(
                            "Transient streaming provider error (attempt {Attempt}/{Max}): {Error} - retrying",
                            transientAttempt, LLMRetryConstants.MaxTransientRetries, streamErrorMessage);
                        await Task.Delay(LLMRetryConstants.GetRetryDelay(transientAttempt), cancellationToken);
                        continue;
                    }

                    yield return SseChunk.Error(streamErrorMessage);
                    yield break;
                }
            }
            else
            {
                var response = await ProcessWithTransientRetryAsync(provider, providerRequest, cancellationToken);
                AccumulateUsage(totalUsage, response.Usage);

                if (!response.Success)
                {
                    yield return SseChunk.Error(response.Error ?? "Provider error");
                    yield break;
                }

                accumulator.AppendContent(response.Content);
                yield return SseChunk.Content(response.Content);

                if (response.FunctionCalls.Any())
                {
                    foreach (var fc in response.FunctionCalls)
                    {
                        accumulator.AppendToolCallDelta(accumulator.FunctionCalls.Count, fc.FunctionName,
                            System.Text.Json.JsonSerializer.Serialize(fc.Parameters));
                    }
                    hasToolEnd = true;
                }
            }

            if (hasToolEnd)
            {
                accumulator.FinalizeFunctionCalls();
            }

            fullResponseContent.Append(accumulator.AccumulatedContent);

            if (!accumulator.HasFunctionCalls)
                break;

            var functionCalls = accumulator.FunctionCalls.ToList();
            allFunctionCalls.AddRange(functionCalls);
            ApplyRecipeInjections(recipePlan, functionCalls);

            foreach (var call in functionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
                yield return SseChunk.FunctionCallChunk(call.FunctionName, call.Parameters);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(context, functionCalls);
            recipePlan?.Observe(functionCalls);
            if (_functionExecutor.NavigationRoute != null)
                navigationRoute = _functionExecutor.NavigationRoute;
            if (_functionExecutor.NavigationTarget != null)
                navigationTarget = _functionExecutor.NavigationTarget;

            foreach (var call in functionCalls)
            {
                var executionType = _functionExecutor.HasOnlyUiPassthroughCalls ? "UiPassthrough" : "Skill";
                yield return SseChunk.FunctionResultChunk(call.FunctionName, call.Result, executionType, call.UiActionSteps);
            }

            if (_functionExecutor.HasOnlyUiPassthroughCalls)
                break;

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            var assistantContent = string.IsNullOrEmpty(accumulator.AccumulatedContent)
                ? "[Executing function calls]"
                : accumulator.AccumulatedContent;
            runningHistory.Add(new Providers.LLMMessage { Role = "assistant", Content = assistantContent });
            currentMessage = FormatFunctionResults(functionCalls);
        }

        if (enginePlan != null && !recipePausedOnAsk && !enginePlan.IsActive)
        {
            _recipeEngine.Clear(recipeUserGuid, conversation!.ConversationId);
        }

        var responseContent = fullResponseContent.ToString();

        // V1 (streaming): the lie is already on screen (content streams token-by-token before the
        // loop ends), so it cannot be retracted — append an honest correction instead. A mutation
        // request that produced zero tool calls means nothing happened, regardless of any prose claim.
        // Also catch the case where intent detection missed the phrasing but the model emitted a
        // text tool-call itself (e.g. "<function_calls>…" for a non-existent skill): that markup never
        // executes, so a zero-real-tool-call turn that contains it is the same no-action lie.
        // A clarifying question (or a [REPLIES:] affordance) is not a false success claim, so skip it —
        // otherwise the well-behaved default path (Gemini/Anthropic ignore tool_choice) would regress.
        // A recipe deliberately paused on an ask is also not a no-action lie — bypass the notice.
        var emittedTextToolCall = ToolCallMarkupSanitizer.ContainsMarkup(responseContent);
        if ((isMutationIntent || forceConfirmation || emittedTextToolCall) && allFunctionCalls.Count == 0
            && !recipePausedOnAsk && !IsClarifyingResponse(responseContent))
        {
            yield return SseChunk.Content(MutationGuardConstants.NoActionStreamNotice);
            responseContent += MutationGuardConstants.NoActionStreamNotice;
        }

        // A forced recipe step (tool_choice=required) can fail every iteration until maxIterations is
        // exhausted — e.g. a name-resolution skill rejecting the model's guess each time. Function-call
        // turns typically carry no prose content, so responseContent stays blank and the user would see
        // literally nothing. Surface the last failure's own message (already actionable, e.g. lists the
        // real options) instead of leaving the chat hanging.
        if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Count > 0
            && allFunctionCalls.All(c => !c.Success))
        {
            var lastFailureNotice = MutationGuardConstants.RecipeStepFailedNoticePrefix + allFunctionCalls[^1].Result;
            yield return SseChunk.Content(lastFailureNotice);
            responseContent += lastFailureNotice;
        }

        try
        {
            await _conversationManager.SaveConversationMessagesAsync(
                conversation!, context.Message, responseContent, model!.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation!,
                totalUsage, stopwatch.ElapsedMilliseconds);

            var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
            _backgroundTaskService.RunBackgroundTasks(agent, conversation!, context, responseContent, allFunctionCalls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stream conversation for user {UserId}", context.UserId);
        }

        var metadataResponse = _responseBuilder.BuildSuccessResponse(
            new LLMProviderResponse { Content = responseContent, Usage = totalUsage, Success = true },
            conversation!.ConversationId, responseContent, allFunctionCalls, navigationRoute, navigationTarget);
        await ApplySuggestionGroundingAsync(metadataResponse, askedSlot, cancellationToken);

        yield return SseChunk.Metadata(metadataResponse);
        yield return SseChunk.Done();
    }

    /// <summary>
    /// Calls the provider and retries on transient failures (rate limit, overload, gateway errors)
    /// with linear backoff. Non-transient errors and exhausted retries return the failed response as-is.
    /// </summary>
    /// <param name="provider">The LLM provider to call</param>
    /// <param name="request">The provider request to (re-)send</param>
    /// <param name="cancellationToken">Cancels the backoff delay between attempts</param>
    internal async Task<LLMProviderResponse> ProcessWithTransientRetryAsync(
        ILLMProvider provider, LLMProviderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await provider.ProcessAsync(request);

        for (var attempt = 1;
             !response.Success
                 && attempt <= LLMRetryConstants.MaxTransientRetries
                 && TransientProviderErrorDetector.IsTransient(response.Error);
             attempt++)
        {
            _logger.LogWarning(
                "Transient provider error (attempt {Attempt}/{Max}): {Error} - retrying",
                attempt, LLMRetryConstants.MaxTransientRetries, response.Error);
            await Task.Delay(LLMRetryConstants.GetRetryDelay(attempt), cancellationToken);
            response = await provider.ProcessAsync(request);
        }

        return response;
    }

    private async Task<(LLMModel? model, ILLMProvider? provider, string? error,
        LLMConversation? conversation, string? systemPrompt, List<Providers.LLMMessage>? history)>
        PrepareContextAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var stageWatch = Stopwatch.StartNew();

        var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(context.ModelId);
        if (error != null) return (null, null, error, null, null, null);

        var conversation = await _conversationManager.GetOrCreateConversationAsync(context.ConversationId, context.UserId);
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);

        stageWatch.Restart();
        var llmHistory = await _conversationManager.GetConversationHistoryAsync(conversation.ConversationId);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "GetConversationHistory", stageWatch.ElapsedMilliseconds);

        string? soulAndMemoryPrompt = null;
        if (agent != null)
        {
            stageWatch.Restart();
            var availableSkillNames = context.AvailableFunctions?.Select(f => f.Name).ToList();
            Guid? userId = Guid.TryParse(context.UserId, out var parsedUserId) ? parsedUserId : null;
            soulAndMemoryPrompt = await _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(
                agent.Id, context.Message, context.Language, availableSkillNames, context.ScopedClientPolicy,
                hasDomainSkillContext: context.HasDomainSkillContext ?? true,
                userId: userId,
                isVoiceMode: context.IsVoiceMode);
            if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
                _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "AssembleSoulAndMemory", stageWatch.ElapsedMilliseconds);
        }

        stageWatch.Restart();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "BuildSystemPrompt", stageWatch.ElapsedMilliseconds);

        var historyBudget = HistoryBudgetFor(provider!, model!, systemPrompt);
        var truncatedHistory = TruncateHistory(llmHistory, historyBudget, conversation.Summary);

        return (model, provider, null, conversation, systemPrompt, truncatedHistory);
    }

    // Effective per-turn budget for conversation history, derived from the provider's real input limit
    // for this model. Shared by the initial truncation and the in-loop re-truncation so both use the
    // exact same ceiling.
    private static int HistoryBudgetFor(ILLMProvider provider, LLMModel model, string? systemPrompt) =>
        ComputeHistoryBudget(
            provider.GetEffectiveInputTokenLimit(model),
            model.MaxTokens,
            EstimateTokens(systemPrompt));

    internal async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls, string? askedSlot)> ExecuteMultiTurnLoopAsync(
        MultiTurnContext ctx)
    {
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var allFunctionCalls = new List<LLMFunctionCall>();
        var runningHistory = new List<Providers.LLMMessage>(ctx.TruncatedHistory);
        var currentMessage = ctx.Context.Message;
        var historyBudget = HistoryBudgetFor(ctx.Provider, ctx.Model, ctx.SystemPrompt);
        string responseContent = "";
        LLMProviderResponse? lastResponse = null;
        int iterationsUsed = 0;
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(ctx.Context.Message);
        var isNavigationIntent = NavigationIntentDetector.IsNavigationIntent(ctx.Context.Message);
        var (forceConfirmation, confirmFunction, pendingNote) = ResolvePendingConfirmation(ctx.Context);
        var enginePlan = await ResolveOrResumeRecipeAsync(
            ctx.Context, ctx.Provider, ctx.Model, ctx.Conversation.ConversationId, CancellationToken.None);
        var cutPlan = enginePlan == null ? RecipeForcingResolver.Resolve(ctx.Context.Message) : null;
        IRecipeForcingPlan? recipePlan = (IRecipeForcingPlan?)enginePlan ?? cutPlan;
        Guid.TryParse(ctx.Context.UserId, out var recipeUserGuid);
        var recipePausedOnAsk = false;
        var forcedRetryUsed = false;
        string? askedSlot = null;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;

            FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);

            enginePlan?.AdvanceOverSatisfied();
            if (enginePlan != null && enginePlan.NeedsConfirmation)
            {
                var confirmInstruction = enginePlan.ConfirmationInstruction;
                var confirmRequest = new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = ctx.SystemPrompt + "\n\n" + confirmInstruction,
                    ModelId = ctx.Model.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = ctx.Model.MaxTokens,
                    CostPerInputToken = ctx.Model.CostPerInputToken,
                    CostPerOutputToken = ctx.Model.CostPerOutputToken
                };

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, confirmRequest);
                AccumulateUsage(ctx.TotalUsage, lastResponse.Usage);
                if (lastResponse.Success)
                {
                    responseContent = RecipeReplyGuard.SafeConfirmation(
                        lastResponse.Content, enginePlan.Goal, enginePlan.AlternativeGoal, ctx.Context.Language,
                        enginePlan.GoalTranslations, enginePlan.AlternativeGoalTranslations);
                }

                _recipeEngine.Persist(recipeUserGuid, ctx.Conversation.ConversationId, enginePlan);
                recipePausedOnAsk = true;
                _logger.LogInformation("Recipe '{Recipe}' paused for confirmation (semantic match)", enginePlan.Name);
                break;
            }

            if (enginePlan != null && enginePlan.IsActive && enginePlan.CurrentIsAsk)
            {
                var askInstruction = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    RecipeEngineDefaults.AskStepInstructionTemplate, enginePlan.CurrentAskPrompt);
                var askRequest = new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = ctx.SystemPrompt + "\n\n" + askInstruction,
                    ModelId = ctx.Model.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = ctx.Model.MaxTokens,
                    CostPerInputToken = ctx.Model.CostPerInputToken,
                    CostPerOutputToken = ctx.Model.CostPerOutputToken
                };

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, askRequest);
                AccumulateUsage(ctx.TotalUsage, lastResponse.Usage);
                if (lastResponse.Success)
                {
                    responseContent = RecipeReplyGuard.SafeAsk(
                        lastResponse.Content, enginePlan.CurrentAskPrompt ?? string.Empty,
                        enginePlan.CurrentAskPromptTranslations, ctx.Context.Language);
                }

                askedSlot = enginePlan.CurrentStep?.Slot;
                _recipeEngine.Persist(recipeUserGuid, ctx.Conversation.ConversationId, enginePlan);
                recipePausedOnAsk = true;
                _logger.LogInformation("Recipe '{Recipe}' paused on ask step (slot {Slot})",
                    enginePlan.Name, askedSlot);
                break;
            }

            var iterationFunctions = iteration == 0
                ? ctx.Context.AvailableFunctions
                : GetReducedFunctions(ctx.Context.AvailableFunctions, calledFunctionNames);

            var confirmThisIteration = forceConfirmation && allFunctionCalls.Count == 0;
            if (confirmThisIteration)
            {
                iterationFunctions = new List<LLMFunction> { confirmFunction! };
            }

            var (forceRecipe, recipeFunctions, recipeNote) = ResolveRecipeIteration(
                recipePlan, confirmThisIteration, ctx.Context.AvailableFunctions, iterationFunctions);
            iterationFunctions = recipeFunctions;
            if (forceRecipe)
            {
                _logger.LogInformation("Recipe forcing engaged ({Recipe}): forcing step skill {Skill} (iteration {Iteration})",
                    recipePlan!.Name, recipePlan.CurrentSkill, iteration);
            }

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = confirmThisIteration ? ctx.SystemPrompt + "\n\n" + pendingNote
                    : forceRecipe ? ctx.SystemPrompt + "\n\n" + recipeNote
                    : ctx.SystemPrompt,
                ModelId = ctx.Model.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = ctx.Model.MaxTokens,
                CostPerInputToken = ctx.Model.CostPerInputToken,
                CostPerOutputToken = ctx.Model.CostPerOutputToken,
                ToolChoice = (forceRecipe || ((isMutationIntent || isNavigationIntent || forceConfirmation) && allFunctionCalls.Count == 0))
                    ? MutationGuardConstants.ToolChoiceRequired
                    : null
            };

            lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, providerRequest);
            AccumulateUsage(ctx.TotalUsage, lastResponse.Usage);

            if (!lastResponse.Success)
            {
                _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
                    iterationsUsed, lastResponse.Error);
                await _conversationManager.TrackUsageAsync(
                    ctx.Context.UserId, ctx.Model, ctx.Conversation,
                    ctx.TotalUsage, ctx.Stopwatch.ElapsedMilliseconds,
                    hasError: true, errorMessage: lastResponse.Error);
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
                if ((isMutationIntent || forceConfirmation || ToolCallMarkupSanitizer.ContainsMarkup(lastResponse.Content))
                    && allFunctionCalls.Count == 0 && !forcedRetryUsed
                    && !recipePausedOnAsk
                    && iteration < maxIterations - 1
                    && !IsClarifyingResponse(lastResponse.Content))
                {
                    forcedRetryUsed = true;
                    runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
                    runningHistory.Add(new Providers.LLMMessage
                    {
                        Role = "assistant",
                        Content = string.IsNullOrWhiteSpace(lastResponse.Content)
                            ? "[no action taken]"
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
            ApplyRecipeInjections(recipePlan, lastResponse.FunctionCalls);
            foreach (var call in lastResponse.FunctionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(ctx.Context, lastResponse.FunctionCalls);
            recipePlan?.Observe(lastResponse.FunctionCalls);

            if (_functionExecutor.HasOnlyUiPassthroughCalls)
            {
                _logger.LogInformation("All function calls are UiPassthrough - breaking multi-turn loop");
                break;
            }

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            var assistantContent = string.IsNullOrEmpty(lastResponse.Content)
                ? "[Executing function calls]"
                : lastResponse.Content;
            runningHistory.Add(new Providers.LLMMessage { Role = "assistant", Content = assistantContent });
            currentMessage = FormatFunctionResults(lastResponse.FunctionCalls);
        }

        if (enginePlan != null && !recipePausedOnAsk && !enginePlan.IsActive)
        {
            _recipeEngine.Clear(recipeUserGuid, ctx.Conversation.ConversationId);
        }

        // Mirrors the streaming loop's guard: a forced recipe step can fail every iteration until
        // maxIterations is exhausted, leaving responseContent blank since function-call turns typically
        // carry no prose. Surface the last failure's own message instead of returning nothing.
        if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Count > 0
            && allFunctionCalls.All(c => !c.Success))
        {
            responseContent = MutationGuardConstants.RecipeStepFailedNoticePrefix + allFunctionCalls[^1].Result;
        }

        if (allFunctionCalls.Count > 0)
        {
            _logger.LogInformation("Multi-turn completed: {TotalCalls} function calls in {Iterations} iterations",
                allFunctionCalls.Count, iterationsUsed);
        }

        return (responseContent, lastResponse, iterationsUsed, allFunctionCalls, askedSlot);
    }

    // A clarifying question or an interactive reply affordance ("[REPLIES:date …]") is the assistant
    // asking for input, not claiming a completed action — so it must NOT trip the no-action V1 guard.
    private static bool IsClarifyingResponse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.TrimEnd().EndsWith("?", StringComparison.Ordinal)
            || content.Contains("[REPLIES:", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFunctionResults(List<LLMFunctionCall> functionCalls)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Function Results]");
        foreach (var call in functionCalls)
        {
            sb.AppendLine($"- {call.FunctionName}: {CapToolResult(call.Result) ?? "OK"}");
        }
        sb.AppendLine("[/Function Results]");
        return sb.ToString();
    }

    // A single skill can return a large payload (e.g. a long list). Fed back verbatim into the loop this
    // would inflate the running history until the prompt exceeds the model's input limit. Cap it so the
    // model still sees the head of the result plus an explicit truncation marker.
    private static string? CapToolResult(string? result)
    {
        if (string.IsNullOrEmpty(result) || result.Length <= MaxToolResultChars)
            return result;

        return result[..MaxToolResultChars]
            + $"\n[Result truncated: {result.Length} chars total, showing first {MaxToolResultChars}.]";
    }

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

    // Data-driven recipe engine entry point (shared by both loops): resume a recipe paused on an ask by
    // raw-filling the current ask slot from the user's message, otherwise match a fresh recipe and
    // pre-fill its slots from the opening message via one structured extraction call. In both cases
    // advance past any already-satisfied steps so the loop sees the next ask (pause) or push (force).
    // A recipe paused on the confirmation gate (semantic match) is a third resume shape: an affirmation
    // clears the gate and proceeds, anything else (rejection, off-topic reply, a question) discards the
    // pending recipe and falls through to a fresh match on the current message instead.
    internal async Task<RecipeExecutionPlan?> ResolveOrResumeRecipeAsync(
        LLMContext context,
        ILLMProvider provider,
        LLMModel model,
        string conversationId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.UserId, out var userGuid))
        {
            return null;
        }

        var resumed = await _recipeEngine.ResumeAsync(userGuid, conversationId, cancellationToken);
        if (resumed != null)
        {
            if (resumed.NeedsConfirmation)
            {
                if (!AffirmationDetector.IsAffirmation(context.Message))
                {
                    _recipeEngine.Clear(userGuid, conversationId);
                    resumed = null;
                }
                else
                {
                    resumed.ConfirmAndProceed();
                    resumed.AdvanceOverSatisfied();
                    return resumed;
                }
            }
            else
            {
                var step = resumed.CurrentStep;
                if (resumed.CurrentIsAsk && !string.IsNullOrWhiteSpace(step?.Slot))
                {
                    // An explicit abort ("abbrechen", "vergiss es", "cancel") must end the recipe, not be
                    // raw-filled into the slot as if it were the answer to the ask question.
                    if (RecipeCancellationDetector.IsCancellation(context.Message))
                    {
                        _recipeEngine.Clear(userGuid, conversationId);
                        _logger.LogInformation(
                            "Recipe '{Recipe}' cancelled by user during ask step (slot {Slot})", resumed.Name, step!.Slot);
                        return null;
                    }

                    resumed.FillSlot(step!.Slot!, context.Message);
                }

                resumed.AdvanceOverSatisfied();
                return resumed;
            }
        }

        var fresh = await _recipeEngine.ResolveAsync(context.Message, context.Language, cancellationToken);
        if (fresh != null)
        {
            var extracted = await _slotExtractor.ExtractAsync(
                provider, model, context.Message, fresh.AskSlotHints(), cancellationToken);
            fresh.PrefillSlots(extracted);
            fresh.AdvanceOverSatisfied();
            _logger.LogInformation(
                "Recipe '{Recipe}' engaged: prefilled slots [{Slots}]", fresh.Name, string.Join(", ", fresh.Slots.Keys));
        }

        return fresh;
    }

    private static List<LLMFunction> GetReducedFunctions(
        List<LLMFunction> allFunctions,
        HashSet<string> calledFunctionNames)
    {
        // Read-only skills and navigation stay available across iterations. Write/CRUD skills
        // stay available too UNLESS they were already called — this keeps multi-step write
        // workflows working (e.g. search_employees then update_client, or create then
        // assign_contract) while still preventing the same side-effecting skill from being
        // invoked twice in one turn.
        return allFunctions
            .Where(f => f.Name.StartsWith(ReadOnlySkillPrefixes.Get) ||
                        f.Name.StartsWith(ReadOnlySkillPrefixes.List) ||
                        f.Name.StartsWith(ReadOnlySkillPrefixes.Search) ||
                        f.Name == SkillNames.NavigateTo ||
                        !calledFunctionNames.Contains(f.Name))
            .ToList();
    }

    private static int EstimateTokens(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : text.Length / CharsPerToken;

    private static int EstimateTokens(IEnumerable<Providers.LLMMessage> messages) =>
        messages.Sum(m => EstimateTokens(m.Content));

    // The token budget available for conversation history (and, in the multi-turn loop, the growing
    // running history + function-result message). Derived from the provider-reported EFFECTIVE input
    // limit for the model so it adapts automatically per model instead of trusting a possibly-inflated
    // nominal context window. Output room, native tool definitions and a fixed interaction headroom are
    // reserved so the context is never filled to the brim.
    private static int ComputeHistoryBudget(int effectiveInputLimit, int maxOutputTokens, int systemPromptTokens)
    {
        var budget = effectiveInputLimit
            - maxOutputTokens
            - systemPromptTokens
            - ToolDefinitionReserveTokens
            - InteractionHeadroomTokens
            - SafetyMarginTokens;

        return Math.Max(budget, MinHistoryBudgetTokens);
    }

    private static List<Providers.LLMMessage> TruncateHistory(
        List<Providers.LLMMessage> history,
        int historyBudgetTokens,
        string? conversationSummary = null)
    {
        var hasSummary = !string.IsNullOrWhiteSpace(conversationSummary);

        var historyBudget = historyBudgetTokens;
        if (hasSummary)
        {
            historyBudget -= EstimateTokens(conversationSummary) + 50;
        }

        if (history.Count <= MaxHistoryMessages && !hasSummary && EstimateTokens(history) <= historyBudget)
            return history;

        var truncated = new List<Providers.LLMMessage>();
        var tokenCount = 0;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(history[i].Content);
            tokenCount += msgTokens;

            if (tokenCount > historyBudget || truncated.Count >= MaxHistoryMessages)
                break;

            truncated.Insert(0, history[i]);
        }

        if (hasSummary)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = "system",
                Content = $"[Conversation Summary (earlier messages)]\n{conversationSummary}\n[/Conversation Summary]"
            });
        }
        else if (truncated.Count < history.Count)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = "system",
                Content = $"[Earlier messages truncated. Showing last {truncated.Count} of {history.Count} messages.]"
            });
        }

        return truncated;
    }

    // Called before every provider call inside the multi-turn tool loop. The running history grows by a
    // user + assistant message each iteration and the function-result message can be large, so without
    // this the accumulated prompt can exceed the model's input limit mid-loop. Drops the oldest
    // non-system messages (a leading conversation-summary system message is preserved) until the
    // estimated running history plus the next message fits the same budget used for the initial history.
    private static void FitRunningHistoryToBudget(
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


    private static void AccumulateUsage(Providers.LLMUsage total, Providers.LLMUsage current)
    {
        total.InputTokens += current.InputTokens;
        total.OutputTokens += current.OutputTokens;
        total.Cost += current.Cost;
    }
}
