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

    private const int MaxHistoryMessages = 20;

    // Conservative estimate of the per-turn prompt overhead the system spends
    // outside of conversation history: identity + ontology (~1500 tk) +
    // permissions + intro + ~15 tool definitions (~1500 tk) + memory block +
    // optional CurrentView block. Real-world: ~5–7k tokens. 8000 leaves ~1k
    // safety margin while giving substantially more headroom for history than
    // the prior 15k did.
    private const int EstimatedOverheadTokens = 8_000;
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
        IPendingConfirmationStore pendingConfirmationStore)
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

            var (responseContent, lastResponse, iterationsUsed, allFunctionCalls) =
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

            return _responseBuilder.BuildSuccessResponse(
                lastResponse!, conversation!.ConversationId, responseContent, allFunctionCalls, _functionExecutor.NavigationRoute);
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
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var firstTokenLogged = false;
        string? navigationRoute = null;
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message);
        var (forceConfirmation, confirmFunction, pendingNote) = ResolvePendingConfirmation(context);

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            var iterationFunctions = iteration == 0
                ? context.AvailableFunctions
                : GetReducedFunctions(context.AvailableFunctions, calledFunctionNames);

            var confirmThisIteration = forceConfirmation && allFunctionCalls.Count == 0;
            if (confirmThisIteration)
            {
                iterationFunctions = new List<LLMFunction> { confirmFunction! };
            }

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = confirmThisIteration ? systemPrompt + "\n\n" + pendingNote : systemPrompt!,
                ModelId = model!.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                Stream = true,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                ToolChoice = ((isMutationIntent || forceConfirmation) && allFunctionCalls.Count == 0)
                    ? MutationGuardConstants.ToolChoiceRequired
                    : null
            };

            var accumulator = new StreamAccumulator();
            var hasToolEnd = false;

            if (provider!.SupportsStreaming)
            {
                string? streamErrorMessage = null;
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
                        yield return SseChunk.Content(token);
                    }
                }

                await enumerator.DisposeAsync();

                if (streamErrorMessage != null)
                {
                    yield return SseChunk.Error(streamErrorMessage);
                    yield break;
                }
            }
            else
            {
                var response = await provider.ProcessAsync(providerRequest);
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

            foreach (var call in functionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
                yield return SseChunk.FunctionCallChunk(call.FunctionName, call.Parameters);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(context, functionCalls);
            if (_functionExecutor.NavigationRoute != null)
                navigationRoute = _functionExecutor.NavigationRoute;

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

        var responseContent = fullResponseContent.ToString();

        // V1 (streaming): the lie is already on screen (content streams token-by-token before the
        // loop ends), so it cannot be retracted — append an honest correction instead. A mutation
        // request that produced zero tool calls means nothing happened, regardless of any prose claim.
        // A clarifying question (or a [REPLIES:] affordance) is not a false success claim, so skip it —
        // otherwise the well-behaved default path (Gemini/Anthropic ignore tool_choice) would regress.
        if ((isMutationIntent || forceConfirmation) && allFunctionCalls.Count == 0 && !IsClarifyingResponse(responseContent))
        {
            yield return SseChunk.Content(MutationGuardConstants.NoActionStreamNotice);
            responseContent += MutationGuardConstants.NoActionStreamNotice;
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
            conversation!.ConversationId, responseContent, allFunctionCalls, navigationRoute);

        yield return SseChunk.Metadata(metadataResponse);
        yield return SseChunk.Done();
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
                userId: userId);
            if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
                _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "AssembleSoulAndMemory", stageWatch.ElapsedMilliseconds);
        }

        stageWatch.Restart();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "BuildSystemPrompt", stageWatch.ElapsedMilliseconds);

        var truncatedHistory = TruncateHistory(llmHistory, model!.ContextWindow, model.MaxTokens, conversation.Summary);

        return (model, provider, null, conversation, systemPrompt, truncatedHistory);
    }

    private async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls)> ExecuteMultiTurnLoopAsync(
        MultiTurnContext ctx)
    {
        const int maxIterations = Klacks.Api.Domain.Constants.LLMLoopConstants.MaxChatToolIterations;
        var allFunctionCalls = new List<LLMFunctionCall>();
        var runningHistory = new List<Providers.LLMMessage>(ctx.TruncatedHistory);
        var currentMessage = ctx.Context.Message;
        string responseContent = "";
        LLMProviderResponse? lastResponse = null;
        int iterationsUsed = 0;
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isMutationIntent = MutationIntentDetector.IsMutationIntent(ctx.Context.Message);
        var (forceConfirmation, confirmFunction, pendingNote) = ResolvePendingConfirmation(ctx.Context);
        var forcedRetryUsed = false;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;

            var iterationFunctions = iteration == 0
                ? ctx.Context.AvailableFunctions
                : GetReducedFunctions(ctx.Context.AvailableFunctions, calledFunctionNames);

            var confirmThisIteration = forceConfirmation && allFunctionCalls.Count == 0;
            if (confirmThisIteration)
            {
                iterationFunctions = new List<LLMFunction> { confirmFunction! };
            }

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = confirmThisIteration ? ctx.SystemPrompt + "\n\n" + pendingNote : ctx.SystemPrompt,
                ModelId = ctx.Model.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = ctx.Model.MaxTokens,
                CostPerInputToken = ctx.Model.CostPerInputToken,
                CostPerOutputToken = ctx.Model.CostPerOutputToken,
                ToolChoice = ((isMutationIntent || forceConfirmation) && allFunctionCalls.Count == 0)
                    ? MutationGuardConstants.ToolChoiceRequired
                    : null
            };

            lastResponse = await ctx.Provider.ProcessAsync(providerRequest);
            AccumulateUsage(ctx.TotalUsage, lastResponse.Usage);

            if (!lastResponse.Success)
            {
                _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
                    iterationsUsed, lastResponse.Error);
                await _conversationManager.TrackUsageAsync(
                    ctx.Context.UserId, ctx.Model, ctx.Conversation,
                    ctx.TotalUsage, ctx.Stopwatch.ElapsedMilliseconds,
                    hasError: true, errorMessage: lastResponse.Error);
                return (lastResponse.Error ?? "An error occurred.", lastResponse, iterationsUsed, allFunctionCalls);
            }

            responseContent = lastResponse.Content;

            if (!lastResponse.FunctionCalls.Any())
            {
                // V1 (non-streaming): nothing is sent to the client until the loop ends, so a false
                // success claim can still be suppressed. If a mutation request produced no tool call,
                // retry ONCE with a forcing nudge (the next request sets tool_choice="required" because
                // allFunctionCalls is still empty) before giving up.
                if ((isMutationIntent || forceConfirmation) && allFunctionCalls.Count == 0 && !forcedRetryUsed
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
            foreach (var call in lastResponse.FunctionCalls)
            {
                calledFunctionNames.Add(call.FunctionName);
            }

            await _functionExecutor.ProcessFunctionCallsAsync(ctx.Context, lastResponse.FunctionCalls);

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

        if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Any())
        {
            responseContent = string.Empty;
        }

        if (allFunctionCalls.Count > 0)
        {
            _logger.LogInformation("Multi-turn completed: {TotalCalls} function calls in {Iterations} iterations",
                allFunctionCalls.Count, iterationsUsed);
        }

        return (responseContent, lastResponse, iterationsUsed, allFunctionCalls);
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
            sb.AppendLine($"- {call.FunctionName}: {call.Result ?? "OK"}");
        }
        sb.AppendLine("[/Function Results]");
        return sb.ToString();
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

    private static List<Providers.LLMMessage> TruncateHistory(
        List<Providers.LLMMessage> history,
        int contextWindow,
        int maxOutputTokens,
        string? conversationSummary = null)
    {
        var hasSummary = !string.IsNullOrWhiteSpace(conversationSummary);

        if (history.Count <= MaxHistoryMessages && !hasSummary)
            return history;

        var availableTokens = contextWindow - maxOutputTokens;
        var estimatedOverheadTokens = EstimatedOverheadTokens;
        var historyBudget = availableTokens - estimatedOverheadTokens;

        if (hasSummary)
        {
            historyBudget -= (conversationSummary!.Length / 4) + 50;
        }

        var truncated = new List<Providers.LLMMessage>();
        var tokenCount = 0;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var msgTokens = (history[i].Content?.Length ?? 0) / 4;
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


    private static void AccumulateUsage(Providers.LLMUsage total, Providers.LLMUsage current)
    {
        total.InputTokens += current.InputTokens;
        total.OutputTokens += current.OutputTokens;
        total.Cost += current.Cost;
    }
}
