// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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

    private const int MaxHistoryMessages = 40;
    private const int ReducedHistoryMessages = 20;
    private const int EstimatedOverheadTokens = 15_000;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        IAgentRepository agentRepository,
        ContextAssemblyPipeline contextAssemblyPipeline,
        ILLMBackgroundTaskService backgroundTaskService)
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
                lastResponse!, conversation!.ConversationId, responseContent, allFunctionCalls);
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
        const int maxIterations = 3;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            var iterationFunctions = iteration == 0
                ? context.AvailableFunctions
                : GetReducedFunctions(context.AvailableFunctions, calledFunctionNames);

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = systemPrompt!,
                ModelId = model!.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                Stream = true,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
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

                    if (token.StartsWith("\0TOOL:"))
                    {
                        var toolJson = token[6..];
                        try
                        {
                            var toolData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(toolJson);
                            var index = toolData.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0;
                            var name = toolData.TryGetProperty("name", out var n) ? n.GetString() : null;
                            var args = toolData.TryGetProperty("arguments", out var a) ? a.GetString() : null;
                            accumulator.AppendToolCallDelta(index, name, args);
                        }
                        catch { }
                    }
                    else if (token == "\0TOOL_END")
                    {
                        hasToolEnd = true;
                    }
                    else
                    {
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
            conversation!.ConversationId, responseContent, allFunctionCalls);

        yield return SseChunk.Metadata(metadataResponse);
        yield return SseChunk.Done();
    }

    private async Task<(LLMModel? model, ILLMProvider? provider, string? error,
        LLMConversation? conversation, string? systemPrompt, List<Providers.LLMMessage>? history)>
        PrepareContextAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var modelTask = _providerOrchestrator.GetModelAndProviderAsync(context.ModelId);
        var conversationTask = _conversationManager.GetOrCreateConversationAsync(context.ConversationId, context.UserId);
        var agentTask = _agentRepository.GetDefaultAgentAsync(cancellationToken);

        await Task.WhenAll(modelTask, conversationTask, agentTask);

        var (model, provider, error) = modelTask.Result;
        if (error != null) return (null, null, error, null, null, null);

        var conversation = conversationTask.Result;
        var agent = agentTask.Result;

        var historyTask = _conversationManager.GetConversationHistoryAsync(conversation.ConversationId);
        var soulTask = agent != null
            ? _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(agent.Id, context.Message, context.Language)
            : Task.FromResult<string?>(null);

        await Task.WhenAll(historyTask, soulTask);

        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulTask.Result);
        var truncatedHistory = TruncateHistory(historyTask.Result, model!.ContextWindow, model.MaxTokens, conversation.Summary);

        return (model, provider, null, conversation, systemPrompt, truncatedHistory);
    }

    private async Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls)> ExecuteMultiTurnLoopAsync(
        MultiTurnContext ctx)
    {
        const int maxIterations = 3;
        var allFunctionCalls = new List<LLMFunctionCall>();
        var runningHistory = new List<Providers.LLMMessage>(ctx.TruncatedHistory);
        var currentMessage = ctx.Context.Message;
        string responseContent = "";
        LLMProviderResponse? lastResponse = null;
        int iterationsUsed = 0;
        var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;

            var iterationFunctions = iteration == 0
                ? ctx.Context.AvailableFunctions
                : GetReducedFunctions(ctx.Context.AvailableFunctions, calledFunctionNames);

            var providerRequest = new LLMProviderRequest
            {
                Message = currentMessage,
                SystemPrompt = ctx.SystemPrompt,
                ModelId = ctx.Model.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = iterationFunctions,
                Temperature = 0.7,
                MaxTokens = ctx.Model.MaxTokens,
                CostPerInputToken = ctx.Model.CostPerInputToken,
                CostPerOutputToken = ctx.Model.CostPerOutputToken
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
        return allFunctions
            .Where(f => f.Name.StartsWith("get_") ||
                        f.Name.StartsWith("list_") ||
                        f.Name.StartsWith("search_") ||
                        f.Name == "navigate_to")
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
