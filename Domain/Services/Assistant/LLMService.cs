using System.Diagnostics;
using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Application.DTOs.Assistant;

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

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        IAgentRepository agentRepository,
        IAgentMemoryRepository agentMemoryRepository,
        ContextAssemblyPipeline contextAssemblyPipeline)
    {
        this._logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
        _agentRepository = agentRepository;
        _agentMemoryRepository = agentMemoryRepository;
        _contextAssemblyPipeline = contextAssemblyPipeline;
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
                    agent.Id, context.Message);
            }

            var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt);

            var allFunctionCalls = new List<LLMFunctionCall>();
            var totalUsage = new LLMUsage();
            var runningHistory = new List<LLMMessage>(llmHistory);
            var currentMessage = context.Message;
            string responseContent = "";
            LLMProviderResponse? lastResponse = null;
            int iterationsUsed = 0;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                iterationsUsed = iteration + 1;

                var providerRequest = new LLMProviderRequest
                {
                    Message = currentMessage,
                    SystemPrompt = systemPrompt,
                    ModelId = model!.ApiModelId,
                    ConversationHistory = runningHistory,
                    AvailableFunctions = context.AvailableFunctions,
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
                await _functionExecutor.ProcessFunctionCallsAsync(context, lastResponse.FunctionCalls);

                if (_functionExecutor.HasOnlyUiPassthroughCalls)
                {
                    _logger.LogInformation("All function calls are UiPassthrough - breaking multi-turn loop");
                    break;
                }

                runningHistory.Add(new LLMMessage { Role = "user", Content = currentMessage });
                var assistantContent = string.IsNullOrEmpty(lastResponse.Content)
                    ? "[Executing function calls]"
                    : lastResponse.Content;
                runningHistory.Add(new LLMMessage { Role = "assistant", Content = assistantContent });
                currentMessage = FormatFunctionResults(lastResponse.FunctionCalls);
            }

            if (string.IsNullOrWhiteSpace(responseContent) && allFunctionCalls.Any())
            {
                responseContent = "The requested actions have been executed.";
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
        sb.AppendLine("If the user's original request is not yet fully completed, continue by calling the next required function. Do NOT just report the results as text.");
        return sb.ToString();
    }

    private static void AccumulateUsage(LLMUsage total, LLMUsage current)
    {
        total.InputTokens += current.InputTokens;
        total.OutputTokens += current.OutputTokens;
        total.Cost += current.Cost;
    }
}
