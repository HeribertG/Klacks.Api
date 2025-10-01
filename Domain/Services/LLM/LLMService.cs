using System.Diagnostics;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMService : ILLMService
{
    private readonly ILogger<LLMService> _logger;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly LLMConversationManager _conversationManager;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly LLMResponseBuilder _responseBuilder;
    private readonly LLMSystemPromptBuilder _promptBuilder;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder)
    {
        _logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context)
    {
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

            var providerRequest = new LLMProviderRequest
            {
                Message = context.Message,
                SystemPrompt = _promptBuilder.BuildSystemPrompt(context),
                ModelId = model!.ApiModelId,
                ConversationHistory = llmHistory,
                AvailableFunctions = context.AvailableFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            var providerResponse = await provider!.ProcessAsync(providerRequest);

            if (!providerResponse.Success)
            {
                _logger.LogError("Provider returned error: {Error}", providerResponse.Error);
                await _conversationManager.TrackUsageAsync(
                    context.UserId, model, conversation,
                    new Providers.LLMUsage(), stopwatch.ElapsedMilliseconds,
                    hasError: true, errorMessage: providerResponse.Error);
                return _responseBuilder.BuildErrorResponse(providerResponse.Error ?? "Ein Fehler ist aufgetreten.");
            }

            var responseContent = providerResponse.Content;
            if (providerResponse.FunctionCalls.Any())
            {
                var functionResults = await _functionExecutor.ProcessFunctionCallsAsync(context, providerResponse.FunctionCalls);
                if (!string.IsNullOrEmpty(functionResults))
                {
                    responseContent += "\n\n" + functionResults;
                }
            }

            await _conversationManager.SaveConversationMessagesAsync(
                conversation, context.Message, responseContent, model.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation,
                providerResponse.Usage, stopwatch.ElapsedMilliseconds);

            return _responseBuilder.BuildSuccessResponse(providerResponse, conversation.ConversationId, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request for user {UserId}", context.UserId);
            return _responseBuilder.BuildErrorResponse("Ein interner Fehler ist aufgetreten.");
        }
    }

}