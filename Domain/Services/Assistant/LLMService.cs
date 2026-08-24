// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
    private readonly IContextBudgetPolicy _contextBudgetPolicy;

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

    // Per function-result cap fed back into the loop, so one huge tool payload cannot blow the budget.
    private const int MaxToolResultChars = 8_000;

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
        IPendingConfirmationStore pendingConfirmationStore,
        RecipeEngineService recipeEngine,
        RecipeSlotExtractor slotExtractor,
        ISuggestionEntityNameReader suggestionEntityNameReader,
        IContextBudgetPolicy contextBudgetPolicy)
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
        _contextBudgetPolicy = contextBudgetPolicy;
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
    /// A pending gate-replay row deliberately overrides a mutation intent in the same message: a reply
    /// that restates the action ("yes, delete the user") is still an answer to the question the gate
    /// asked. Vetoing it here made the model re-call the skill, which produced a fresh hold and a
    /// confirmation loop. Only the first iteration is narrowed, so any additional request in the same
    /// message is still served once the token is redeemed.
    /// </summary>
    internal (bool Force, LLMFunction? ConfirmFunction, string? ContextNote) ResolvePendingConfirmation(LLMContext context)
    {
        if (!AffirmationDetector.IsAffirmation(context.Message)
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
                toolsetAssemblyMs: context.ToolsetAssemblyMs, toolIterations: iterationsUsed);

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
            LLMConversation? conversation, string? systemPrompt, string? volatilePrompt,
            List<Providers.LLMMessage>? truncatedHistory, ContextBudgetProfile? budgetProfile) prepared = default;

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

        var (model, provider, prepError, conversation, systemPrompt, volatilePrompt, history, budgetProfile) = prepared;

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
        var historyBudget = HistoryBudgetFor(provider!, model!, systemPrompt, volatilePrompt, context.AvailableFunctions);
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var firstTokenLogged = false;
        long? ttftMs = null;
        var toolIterationsRun = 0;
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
        var suggestPlan = PlanTriggerHeuristic.IsPlanCandidate(context.Message, recipePlan != null);
        Guid.TryParse(context.UserId, out var recipeUserGuid);
        var recipePausedOnAsk = false;
        string? askedSlot = null;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            toolIterationsRun = iteration + 1;
            FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);

            enginePlan?.AdvanceOverSatisfied();
            if (enginePlan != null && enginePlan.NeedsConfirmation)
            {
                var confirmInstruction = enginePlan.ConfirmationInstruction;
                var confirmResponse = await ProcessWithTransientRetryAsync(provider!, new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = systemPrompt!,
                    VolatileSystemPrompt = CombineVolatile(volatilePrompt, confirmInstruction),
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = model.MaxTokens,
                    SupportedParameters = model.SupportedParameters,
                    CostPerInputToken = model.CostPerInputToken,
                    CostPerOutputToken = model.CostPerOutputToken,
                    CostPerCacheWriteToken = model.CostPerCacheWriteToken,
                    CostPerCacheReadToken = model.CostPerCacheReadToken
                }, cancellationToken);
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
                    SystemPrompt = systemPrompt!,
                    VolatileSystemPrompt = CombineVolatile(volatilePrompt, askInstruction),
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = model.MaxTokens,
                    SupportedParameters = model.SupportedParameters,
                    CostPerInputToken = model.CostPerInputToken,
                    CostPerOutputToken = model.CostPerOutputToken,
                    CostPerCacheWriteToken = model.CostPerCacheWriteToken,
                    CostPerCacheReadToken = model.CostPerCacheReadToken
                }, cancellationToken);
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

            // The full toolset is sent on every iteration (except when it is deliberately narrowed
            // for a forced confirmation or a recipe-forcing step below). Shrinking it per iteration
            // (the previous behaviour) changed the tool array mid-turn and invalidated the provider's
            // prompt-prefix cache on every follow-up call — for every provider with prefix caching,
            // not just one. The once-per-turn rule for write skills is enforced at execution time
            // instead (RejectRepeatedWriteCalls), which is also stricter: a hallucinated repeat call
            // is now rejected rather than silently executed.
            var iterationFunctions = context.AvailableFunctions;

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
                SystemPrompt = systemPrompt!,
                VolatileSystemPrompt = CombineVolatile(volatilePrompt,
                    confirmThisIteration ? pendingNote
                        : forceRecipe ? recipeNote
                        : suggestPlan && allFunctionCalls.Count == 0
                            ? Klacks.Api.Domain.Constants.PlanSkillDefaults.PlanNudgeNote
                            : null),
                ModelId = model!.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                Stream = true,
                SupportedParameters = model.SupportedParameters,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                CostPerCacheWriteToken = model.CostPerCacheWriteToken,
                CostPerCacheReadToken = model.CostPerCacheReadToken,
                ToolChoice = ToolChoicePolicy.ResolveToolChoice(
                    forceRecipe, isMutationIntent, isNavigationIntent, forceConfirmation, allFunctionCalls.Count),
                OnStreamUsage = usage => AccumulateUsage(totalUsage, usage)
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
                                ttftMs = stopwatch.ElapsedMilliseconds;
                                _logger.LogInformation("LLM TTFT: {Ms}ms", ttftMs);
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

            var executableCalls = RejectRepeatedWriteCalls(functionCalls, calledFunctionNames, forceRecipe);

            foreach (var call in functionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
                yield return SseChunk.FunctionCallChunk(call.FunctionName, call.Parameters);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(context, executableCalls);
            recipePlan?.Observe(functionCalls);
            if (functionCalls.Any(c => c.RequiresConfirmation))
            {
                _logger.LogInformation(
                    "Recipe forcing released: a skill was held by the autonomy gate — the model must now ask the user");
                recipePlan = null;
            }

            if (_functionExecutor.NavigationRoute != null)
                navigationRoute = _functionExecutor.NavigationRoute;
            if (_functionExecutor.NavigationTarget != null)
                navigationTarget = _functionExecutor.NavigationTarget;

            foreach (var call in functionCalls)
            {
                // Same vacuous-truth guard as the break below: with an empty execution list
                // HasOnlyUiPassthroughCalls is true although nothing UiPassthrough ran.
                var executionType = executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls
                    ? "UiPassthrough"
                    : "Skill";
                yield return SseChunk.FunctionResultChunk(call.FunctionName, call.Result, executionType, call.UiActionSteps);
            }

            // Guarded on executableCalls: with an empty execution list HasOnlyUiPassthroughCalls is
            // vacuously true and would end the turn before the model ever saw the rejection results.
            if (executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls)
                break;

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            var assistantContent = string.IsNullOrEmpty(accumulator.AccumulatedContent)
                ? "[Executing function calls]"
                : accumulator.AccumulatedContent;
            runningHistory.Add(new Providers.LLMMessage { Role = "assistant", Content = assistantContent });
            currentMessage = FormatFunctionResults(functionCalls, budgetProfile?.MaxToolResultChars);
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
        var claimsCompletion = CompletionClaimDetector.ClaimsCompletion(responseContent);
        if (NoActionNoticePolicy.ShouldAppendNotice(
                isMutationIntent, forceConfirmation, emittedTextToolCall, claimsCompletion,
                allFunctionCalls.Count, recipePausedOnAsk, IsClarifyingResponse(responseContent)))
        {
            yield return SseChunk.Content(MutationGuardConstants.NoActionStreamNotice);
            responseContent += MutationGuardConstants.NoActionStreamNotice;
        }

        // A forced recipe step (tool_choice=required) can fail every iteration until maxIterations is
        // exhausted — e.g. a name-resolution skill rejecting the model's guess each time. Function-call
        // turns typically carry no prose content, so responseContent stays blank and the user would see
        // literally nothing. Surface the last failure's own message (already actionable, e.g. lists the
        // real options) instead of leaving the chat hanging. This is the only user-visible text that
        // bypasses the model entirely, so no prompt rule can strip internal names from it — redact them
        // here and keep the raw message for the log.
        if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Count > 0
            && allFunctionCalls.All(c => !c.Success))
        {
            // Prefer the last REAL failure: a rejected repeat carries only the generic rejection
            // text, while the genuine failure from an earlier iteration is the actionable message.
            var lastFailedCall = allFunctionCalls.LastOrDefault(c => !c.IsRejectedRepeat) ?? allFunctionCalls[^1];
            _logger.LogWarning(
                "All function calls failed in stream turn; surfacing notice for {FunctionName}. Raw result: {RawResult}",
                lastFailedCall.FunctionName, lastFailedCall.Result);
            var lastFailureNotice = MutationGuardConstants.RecipeStepFailedNoticePrefix
                + InternalIdentifierRedactor.Redact(lastFailedCall.Result);
            yield return SseChunk.Content(lastFailureNotice);
            responseContent += lastFailureNotice;
        }

        try
        {
            await _conversationManager.SaveConversationMessagesAsync(
                conversation!, context.Message, responseContent, model!.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation!,
                totalUsage, stopwatch.ElapsedMilliseconds,
                ttftMs: ttftMs, toolsetAssemblyMs: context.ToolsetAssemblyMs, toolIterations: toolIterationsRun);

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
        var response = await provider.ProcessAsync(request, cancellationToken);

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
            response = await provider.ProcessAsync(request, cancellationToken);
        }

        return response;
    }

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
        var volatilePrompt = CombineVolatile(LLMSystemPromptBuilder.BuildVolatileAdditions(context), soulAndMemoryPrompt?.VolatilePrompt);
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

    // Effective per-turn budget for conversation history, derived from the provider's real input limit
    // for this model. Shared by the initial truncation and the in-loop re-truncation so both use the
    // exact same ceiling. Sums the stable and volatile system-prompt segments so the budget reflects
    // the full prompt actually sent to the provider, regardless of how it is split into cache blocks.
    // availableFunctions is the toolset SkillToolsetAssembler already assembled for this turn (final
    // by the time budgeting runs in both call sites), so the tool-definition reserve reflects what is
    // actually sent instead of a pessimistic flat constant.
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
        var (forceConfirmation, confirmFunction, pendingNote) = ResolvePendingConfirmation(ctx.Context);
        var enginePlan = await ResolveOrResumeRecipeAsync(
            ctx.Context, ctx.Provider, ctx.Model, ctx.Conversation.ConversationId, ctx.CancellationToken);
        var cutPlan = enginePlan == null ? RecipeForcingResolver.Resolve(ctx.Context.Message) : null;
        IRecipeForcingPlan? recipePlan = (IRecipeForcingPlan?)enginePlan ?? cutPlan;
        var suggestPlan = PlanTriggerHeuristic.IsPlanCandidate(ctx.Context.Message, recipePlan != null);
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
                    SystemPrompt = ctx.SystemPrompt,
                    VolatileSystemPrompt = CombineVolatile(ctx.VolatilePrompt, confirmInstruction),
                    ModelId = ctx.Model.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = ctx.Model.MaxTokens,
                    SupportedParameters = ctx.Model.SupportedParameters,
                    CostPerInputToken = ctx.Model.CostPerInputToken,
                    CostPerOutputToken = ctx.Model.CostPerOutputToken,
                    CostPerCacheWriteToken = ctx.Model.CostPerCacheWriteToken,
                    CostPerCacheReadToken = ctx.Model.CostPerCacheReadToken
                };

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, confirmRequest, ctx.CancellationToken);
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
                    SystemPrompt = ctx.SystemPrompt,
                    VolatileSystemPrompt = CombineVolatile(ctx.VolatilePrompt, askInstruction),
                    ModelId = ctx.Model.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = new List<LLMFunction>(),
                    Temperature = 0.7,
                    MaxTokens = ctx.Model.MaxTokens,
                    SupportedParameters = ctx.Model.SupportedParameters,
                    CostPerInputToken = ctx.Model.CostPerInputToken,
                    CostPerOutputToken = ctx.Model.CostPerOutputToken,
                    CostPerCacheWriteToken = ctx.Model.CostPerCacheWriteToken,
                    CostPerCacheReadToken = ctx.Model.CostPerCacheReadToken
                };

                lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, askRequest, ctx.CancellationToken);
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

            // Same stability rule as the streaming loop: the toolset never changes across iterations
            // (prompt-prefix cache), repeats of write skills are rejected at execution time instead.
            var iterationFunctions = ctx.Context.AvailableFunctions;

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
                SystemPrompt = ctx.SystemPrompt,
                VolatileSystemPrompt = CombineVolatile(ctx.VolatilePrompt,
                    confirmThisIteration ? pendingNote
                        : forceRecipe ? recipeNote
                        : suggestPlan && allFunctionCalls.Count == 0
                            ? Klacks.Api.Domain.Constants.PlanSkillDefaults.PlanNudgeNote
                            : null),
                ModelId = ctx.Model.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = ctx.Model.MaxTokens,
                SupportedParameters = ctx.Model.SupportedParameters,
                CostPerInputToken = ctx.Model.CostPerInputToken,
                CostPerOutputToken = ctx.Model.CostPerOutputToken,
                CostPerCacheWriteToken = ctx.Model.CostPerCacheWriteToken,
                CostPerCacheReadToken = ctx.Model.CostPerCacheReadToken,
                ToolChoice = ToolChoicePolicy.ResolveToolChoice(
                    forceRecipe, isMutationIntent, isNavigationIntent, forceConfirmation, allFunctionCalls.Count)
            };

            lastResponse = await ProcessWithTransientRetryAsync(ctx.Provider, providerRequest, ctx.CancellationToken);
            AccumulateUsage(ctx.TotalUsage, lastResponse.Usage);

            if (!lastResponse.Success)
            {
                _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
                    iterationsUsed, lastResponse.Error);
                await _conversationManager.TrackUsageAsync(
                    ctx.Context.UserId, ctx.Model, ctx.Conversation,
                    ctx.TotalUsage, ctx.Stopwatch.ElapsedMilliseconds,
                    hasError: true, errorMessage: lastResponse.Error,
                    toolsetAssemblyMs: ctx.Context.ToolsetAssemblyMs, toolIterations: iterationsUsed);
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
                        isMutationIntent, forceConfirmation,
                        ToolCallMarkupSanitizer.ContainsMarkup(lastResponse.Content),
                        CompletionClaimDetector.ClaimsCompletion(lastResponse.Content),
                        allFunctionCalls.Count, recipePausedOnAsk, IsClarifyingResponse(lastResponse.Content))
                    && !forcedRetryUsed
                    && iteration < maxIterations - 1)
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

            var executableCalls = RejectRepeatedWriteCalls(
                lastResponse.FunctionCalls, calledFunctionNames, forceRecipe);

            foreach (var call in lastResponse.FunctionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(ctx.Context, executableCalls);
            recipePlan?.Observe(lastResponse.FunctionCalls);
            if (lastResponse.FunctionCalls.Any(c => c.RequiresConfirmation))
            {
                _logger.LogInformation(
                    "Recipe forcing released: a skill was held by the autonomy gate — the model must now ask the user");
                recipePlan = null;
            }

            if (executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls)
            {
                _logger.LogInformation("All function calls are UiPassthrough - breaking multi-turn loop");
                break;
            }

            runningHistory.Add(new Providers.LLMMessage { Role = "user", Content = currentMessage });
            var assistantContent = string.IsNullOrEmpty(lastResponse.Content)
                ? "[Executing function calls]"
                : lastResponse.Content;
            runningHistory.Add(new Providers.LLMMessage { Role = "assistant", Content = assistantContent });
            currentMessage = FormatFunctionResults(lastResponse.FunctionCalls, ctx.BudgetProfile?.MaxToolResultChars);
        }

        if (enginePlan != null && !recipePausedOnAsk && !enginePlan.IsActive)
        {
            _recipeEngine.Clear(recipeUserGuid, ctx.Conversation.ConversationId);
        }

        // Mirrors the streaming loop's guard: a forced recipe step can fail every iteration until
        // maxIterations is exhausted, leaving responseContent blank since function-call turns typically
        // carry no prose. Surface the last failure's own message instead of returning nothing — redacted
        // the same way, since this text also reaches the user without passing through the model.
        if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Count > 0
            && allFunctionCalls.All(c => !c.Success))
        {
            var lastFailedCall = allFunctionCalls.LastOrDefault(c => !c.IsRejectedRepeat) ?? allFunctionCalls[^1];
            _logger.LogWarning(
                "All function calls failed in multi-turn loop; surfacing notice for {FunctionName}. Raw result: {RawResult}",
                lastFailedCall.FunctionName, lastFailedCall.Result);
            responseContent = MutationGuardConstants.RecipeStepFailedNoticePrefix
                + InternalIdentifierRedactor.Redact(lastFailedCall.Result);
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

    // Prompt-injection containment. Tool results are fed back as a "user" message, so anything they
    // contain reads to the model like input from this system. Three measures apply here:
    // (1) every result gets its own [Result: name] … [/Result] frame, so a newline inside a result can
    //     no longer forge a sibling entry the way the former "- name: result" line format allowed;
    // (2) the skill name and the result body are escaped against all four delimiters, so content cannot
    //     close its own frame or open a new one — this covers trusted skills too, because ERP-imported
    //     and user-entered strings flow through ordinary read skills;
    // (3) results from skills whose content is authored outside this system are flagged untrusted and
    //     carry an explicit data-not-instructions notice, matched by the system prompt's
    //     UNTRUSTED TOOL CONTENT rule.
    // Internal (not private): covered by LLMServiceFormatFunctionResultsTests.
    internal static string FormatFunctionResults(List<LLMFunctionCall> functionCalls, int? maxToolResultChars = null)
    {
        var effectiveMaxToolResultChars = maxToolResultChars ?? MaxToolResultChars;
        var sb = new StringBuilder();
        sb.AppendLine(ToolResultMarkers.BlockHeader);
        foreach (var call in functionCalls)
        {
            var isUntrusted = UntrustedSkillOutputs.Contains(call.FunctionName);

            // Escape BEFORE capping: escaping replaces a 9-character delimiter with a 16-character
            // placeholder, so capping first would let a result built from repeated forged delimiters
            // grow ~1.8x past MaxToolResultChars — attacker-controlled history inflation, which is the
            // very thing the cap exists to prevent.
            var body = call.Result is null
                ? ToolResultMarkers.EmptyResultPlaceholder
                : CapToolResult(ToolResultSanitizer.EscapeDelimiters(call.Result), effectiveMaxToolResultChars)
                  ?? ToolResultMarkers.EmptyResultPlaceholder;

            sb.Append(ToolResultMarkers.ResultOpenPrefix);
            sb.Append(ToolResultSanitizer.EscapeDelimiters(call.FunctionName));
            if (isUntrusted)
            {
                sb.Append(ToolResultMarkers.ResultUntrustedFlag);
            }

            sb.AppendLine(ToolResultMarkers.ResultOpenSuffix);

            if (isUntrusted)
            {
                sb.AppendLine(ToolResultMarkers.UntrustedContentNotice);
            }

            sb.AppendLine(body);
            sb.AppendLine(ToolResultMarkers.ResultClose);
        }

        sb.AppendLine(ToolResultMarkers.BlockFooter);
        return sb.ToString();
    }

    // A single skill can return a large payload (e.g. a long list). Fed back verbatim into the loop this
    // would inflate the running history until the prompt exceeds the model's input limit. Cap it so the
    // model still sees the head of the result plus an explicit truncation marker.
    private static string? CapToolResult(string? result, int maxToolResultChars)
    {
        if (string.IsNullOrEmpty(result) || result.Length <= maxToolResultChars)
            return result;

        return result[..maxToolResultChars]
            + $"\n[Result truncated: {result.Length} chars total, showing first {maxToolResultChars}.]";
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

        var fresh = await _recipeEngine.ResolveAsync(context.Message, context.Language, context.UserRights, cancellationToken);
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

    // Execution-time replacement for the former per-iteration toolset shrinking: read-only skills
    // and navigation may repeat freely, a side-effecting skill already called in an EARLIER
    // iteration must not run twice in one turn (multiple calls within the same batch stay allowed,
    // matching the old semantics). Rejected calls keep flowing through the result pipeline with an
    // instructive message so the model corrects itself on the next iteration. A recipe-forced
    // iteration is exempt: the forcing spine may deliberately re-run a step skill and its calls are
    // narrowed deterministically, not chosen by the model.
    internal static List<LLMFunctionCall> RejectRepeatedWriteCalls(
        List<LLMFunctionCall> functionCalls,
        HashSet<string> previouslyCalledNames,
        bool forceRecipe)
    {
        if (forceRecipe || previouslyCalledNames.Count == 0)
        {
            return functionCalls;
        }

        var executable = new List<LLMFunctionCall>(functionCalls.Count);
        foreach (var call in functionCalls)
        {
            var isReadOnlyOrNavigation =
                ReadOnlySkillPrefixes.HasReadOnlyPrefix(call.FunctionName) ||
                string.Equals(call.FunctionName, SkillNames.NavigateTo, StringComparison.OrdinalIgnoreCase);

            if (!isReadOnlyOrNavigation && previouslyCalledNames.Contains(call.FunctionName))
            {
                call.Success = false;
                call.IsRejectedRepeat = true;
                call.Result = Klacks.Api.Domain.Constants.LLMLoopConstants.RepeatedWriteCallRejectedResult;
            }
            else
            {
                executable.Add(call);
            }
        }

        return executable;
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
        total.CacheCreationInputTokens += current.CacheCreationInputTokens;
        total.CacheReadInputTokens += current.CacheReadInputTokens;
        total.Cost += current.Cost;
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
