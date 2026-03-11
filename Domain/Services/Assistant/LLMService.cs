// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
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
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly ContextAssemblyPipeline _contextAssemblyPipeline;
    private readonly IAutoMemoryExtractionService _autoMemoryExtraction;
    private readonly ISkillGapDetector _skillGapDetector;

    private const int MaxHistoryMessages = 40;
    private const int ReducedHistoryMessages = 20;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        IAgentRepository agentRepository,
        IAgentMemoryRepository agentMemoryRepository,
        ContextAssemblyPipeline contextAssemblyPipeline,
        IAutoMemoryExtractionService autoMemoryExtraction,
        ISkillGapDetector skillGapDetector)
    {
        _logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
        _agentRepository = agentRepository;
        _agentMemoryRepository = agentMemoryRepository;
        _contextAssemblyPipeline = contextAssemblyPipeline;
        _autoMemoryExtraction = autoMemoryExtraction;
        _skillGapDetector = skillGapDetector;
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context)
    {
        const int maxIterations = 5;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId}: {Message}",
                context.UserId, context.Message);

            var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(context.ModelId);
            if (error != null)
            {
                return _responseBuilder.BuildErrorResponse(error);
            }

            var conversation = await _conversationManager.GetOrCreateConversationAsync(
                context.ConversationId, context.UserId);

            var llmHistory = await _conversationManager.GetConversationHistoryAsync(conversation.ConversationId);

            var agent = await _agentRepository.GetDefaultAgentAsync();
            string? soulAndMemoryPrompt = null;

            if (agent != null)
            {
                soulAndMemoryPrompt = await _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(
                    agent.Id, context.Message, context.Language);
            }

            var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt);

            var allFunctionCalls = new List<LLMFunctionCall>();
            var totalUsage = new Providers.LLMUsage();
            var truncatedHistory = TruncateHistory(llmHistory, model!.ContextWindow, model.MaxTokens);
            var runningHistory = new List<Providers.LLMMessage>(truncatedHistory);
            var currentMessage = context.Message;
            string responseContent = "";
            LLMProviderResponse? lastResponse = null;
            int iterationsUsed = 0;
            var calledFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                iterationsUsed = iteration + 1;

                var iterationFunctions = iteration == 0
                    ? context.AvailableFunctions
                    : GetReducedFunctions(context.AvailableFunctions, calledFunctionNames);

                var providerRequest = new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = systemPrompt,
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = iterationFunctions,
                    Temperature = 0.7,
                    MaxTokens = model.MaxTokens,
                    CostPerInputToken = model.CostPerInputToken,
                    CostPerOutputToken = model.CostPerOutputToken
                };

                lastResponse = await provider!.ProcessAsync(providerRequest);
                AccumulateUsage(totalUsage, lastResponse.Usage);

                if (!lastResponse.Success)
                {
                    _logger.LogError("Provider returned error in iteration {Iteration}: {Error}",
                        iterationsUsed, lastResponse.Error);
                    await _conversationManager.TrackUsageAsync(
                        context.UserId, model, conversation,
                        totalUsage, stopwatch.ElapsedMilliseconds,
                        hasError: true, errorMessage: lastResponse.Error);
                    return _responseBuilder.BuildErrorResponse(lastResponse.Error ?? "An error occurred.");
                }

                responseContent = lastResponse.Content;

                if (!lastResponse.FunctionCalls.Any())
                    break;

                _logger.LogInformation("Multi-turn iteration {Iteration}: executing {Count} function calls",
                    iterationsUsed, lastResponse.FunctionCalls.Count);

                allFunctionCalls.AddRange(lastResponse.FunctionCalls);
                foreach (var call in lastResponse.FunctionCalls)
                {
                    calledFunctionNames.Add(call.FunctionName);
                }

                await _functionExecutor.ProcessFunctionCallsAsync(context, lastResponse.FunctionCalls);

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

            await _conversationManager.SaveConversationMessagesAsync(
                conversation, context.Message, responseContent, model!.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation,
                totalUsage, stopwatch.ElapsedMilliseconds);

            if (agent != null && !string.IsNullOrWhiteSpace(responseContent))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _autoMemoryExtraction.ExtractAndStoreMemoriesAsync(
                            agent.Id, context.Message, responseContent, context.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Fire-and-forget auto memory extraction failed for agent {AgentId}", agent.Id);
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _skillGapDetector.DetectAndSuggestAsync(
                            agent.Id, context.Message, responseContent, allFunctionCalls.Count > 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Fire-and-forget skill gap detection failed for agent {AgentId}", agent.Id);
                    }
                });
            }

            return _responseBuilder.BuildSuccessResponse(
                lastResponse!, conversation.ConversationId, responseContent, allFunctionCalls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request for user {UserId}", context.UserId);
            return _responseBuilder.BuildErrorResponse("An internal error occurred.");
        }
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
            .Where(f => calledFunctionNames.Contains(f.Name) ||
                        f.Name.StartsWith("get_") ||
                        f.Name.StartsWith("list_") ||
                        f.Name.StartsWith("search_") ||
                        f.Name == "navigate_to")
            .ToList();
    }

    private static List<Providers.LLMMessage> TruncateHistory(
        List<Providers.LLMMessage> history,
        int contextWindow,
        int maxOutputTokens)
    {
        if (history.Count <= MaxHistoryMessages)
            return history;

        var availableTokens = contextWindow - maxOutputTokens;
        var estimatedOverheadTokens = 15000;
        var historyBudget = availableTokens - estimatedOverheadTokens;

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

        if (truncated.Count < history.Count)
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
