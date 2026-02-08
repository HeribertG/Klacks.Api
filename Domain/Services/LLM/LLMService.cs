using System.Diagnostics;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
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
    private readonly ISettingsRepository _settingsRepository;
    private readonly IAiMemoryRepository _aiMemoryRepository;

    public LLMService(
        ILogger<LLMService> logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder,
        ISettingsRepository settingsRepository,
        IAiMemoryRepository aiMemoryRepository)
    {
        this._logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _promptBuilder = promptBuilder;
        _settingsRepository = settingsRepository;
        _aiMemoryRepository = aiMemoryRepository;
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

            var soul = await LoadSoulAsync();
            var memories = await _aiMemoryRepository.GetAllAsync();

            var providerRequest = new LLMProviderRequest
            {
                Message = context.Message,
                SystemPrompt = _promptBuilder.BuildSystemPrompt(context, soul, memories),
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
                    var summaryResponse = await GenerateSummaryAsync(
                        provider!, providerRequest, providerResponse, functionResults, llmHistory, context.Message);

                    if (summaryResponse is { Success: true })
                    {
                        responseContent = summaryResponse.Content;
                        providerResponse.Usage.InputTokens += summaryResponse.Usage.InputTokens;
                        providerResponse.Usage.OutputTokens += summaryResponse.Usage.OutputTokens;
                        providerResponse.Usage.Cost += summaryResponse.Usage.Cost;
                    }
                    else
                    {
                        responseContent = providerResponse.Content;
                        _logger.LogWarning("Summary generation failed, using original response without function results");
                    }
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

    private async Task<LLMProviderResponse?> GenerateSummaryAsync(
        ILLMProvider provider,
        LLMProviderRequest originalRequest,
        LLMProviderResponse firstResponse,
        string functionResults,
        List<LLMMessage> history,
        string userMessage)
    {
        try
        {
            var updatedHistory = new List<LLMMessage>(history)
            {
                new() { Role = "user", Content = userMessage },
                new() { Role = "assistant", Content = firstResponse.Content }
            };

            var summaryRequest = new LLMProviderRequest
            {
                Message = $"[Funktionsergebnisse]\n{functionResults}\n[/Funktionsergebnisse]\n\n" +
                          "Fasse die obigen Funktionsergebnisse in natürlicher Sprache für den Benutzer zusammen. " +
                          "Antworte in der Sprache des Benutzers. " +
                          "Zeige KEINE technischen Details wie IDs, JSON-Daten, rohe Objekte oder Funktionsnamen. " +
                          "Formuliere die Antwort so, als würdest du direkt mit dem Benutzer sprechen.",
                SystemPrompt = originalRequest.SystemPrompt,
                ModelId = originalRequest.ModelId,
                ConversationHistory = updatedHistory,
                AvailableFunctions = new List<LLMFunction>(),
                Temperature = originalRequest.Temperature,
                MaxTokens = originalRequest.MaxTokens,
                CostPerInputToken = originalRequest.CostPerInputToken,
                CostPerOutputToken = originalRequest.CostPerOutputToken
            };

            _logger.LogInformation("Generating human-friendly summary for function results");
            return await provider.ProcessAsync(summaryRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for function results");
            return null;
        }
    }

    private async Task<string?> LoadSoulAsync()
    {
        try
        {
            var soulSetting = await _settingsRepository.GetSetting(Application.Constants.Settings.AI_SOUL);
            return soulSetting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load AI soul from settings");
            return null;
        }
    }
}
