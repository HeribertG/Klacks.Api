using System.Diagnostics;
using System.Text;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMService : ILLMService
{
    private readonly ILogger<LLMService> _logger;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly LLMConversationManager _conversationManager;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly LLMResponseBuilder _responseBuilder;
    private readonly LLMSystemPromptBuilder _promptBuilder;
    private readonly IAiSoulRepository _aiSoulRepository;
    private readonly IAiMemoryRepository _aiMemoryRepository;
    private readonly IAiGuidelinesRepository _aiGuidelinesRepository;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        IAiSoulRepository aiSoulRepository,
        IAiMemoryRepository aiMemoryRepository,
        IAiGuidelinesRepository aiGuidelinesRepository)
    {
        this._logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
        _aiSoulRepository = aiSoulRepository;
        _aiMemoryRepository = aiMemoryRepository;
        _aiGuidelinesRepository = aiGuidelinesRepository;
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

            var soul = await LoadSoulAsync();
            var memories = await _aiMemoryRepository.GetAllAsync();
            var guidelines = await LoadGuidelinesAsync();

            var systemPrompt = _promptBuilder.BuildSystemPrompt(context, soul, memories, guidelines);

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

                runningHistory.Add(new LLMMessage { Role = "user", Content = currentMessage });
                runningHistory.Add(new LLMMessage { Role = "assistant", Content = lastResponse.Content });
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
        return sb.ToString();
    }

    private static void AccumulateUsage(LLMUsage total, LLMUsage current)
    {
        total.InputTokens += current.InputTokens;
        total.OutputTokens += current.OutputTokens;
        total.Cost += current.Cost;
    }

    private async Task<string?> LoadSoulAsync()
    {
        try
        {
            var activeSoul = await _aiSoulRepository.GetActiveAsync();
            return activeSoul?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load AI soul");
            return null;
        }
    }

    private async Task<string?> LoadGuidelinesAsync()
    {
        try
        {
            var activeGuidelines = await _aiGuidelinesRepository.GetActiveAsync();
            return activeGuidelines?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load AI guidelines");
            return null;
        }
    }
}
