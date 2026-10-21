// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Entry point of a chat turn for both chat paths: prepares the turn's context, answers a graceful
/// correction with its clarification question, runs the non-streaming tool loop or streams the turn,
/// and persists the non-streaming turn. The work is done by the ChatTurn collaborators, which are built
/// here from the injected services and log through this service's logger, so the DI graph and the log
/// categories stay those of the chat service.
/// </summary>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Services.Assistant.ChatTurn;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMService : ILLMService
{
    private readonly ILogger<LLMService> _logger;
    private readonly LLMConversationManager _conversationManager;
    private readonly LLMFunctionExecutor _functionExecutor;
    private readonly LLMResponseBuilder _responseBuilder;
    private readonly IAgentRepository _agentRepository;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly TurnRunState _turnState;
    private readonly ChatTurnContextPreparer _contextPreparer;
    private readonly ClarificationTurnResponder _clarificationResponder;
    private readonly SuggestionGrounding _suggestionGrounding;
    private readonly MultiTurnChatLoop _multiTurnLoop;
    private readonly StreamedChatTurn _streamedTurn;

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
        _conversationManager = conversationManager;
        _functionExecutor = functionExecutor;
        _responseBuilder = responseBuilder;
        _agentRepository = agentRepository;
        _backgroundTaskService = backgroundTaskService;
        _turnPreparation = turnPreparation;
        _turnState = turnState;

        _contextPreparer = new ChatTurnContextPreparer(
            logger, providerOrchestrator, conversationManager, agentRepository, contextAssemblyPipeline,
            promptBuilder, contextBudgetPolicy);
        _clarificationResponder = new ClarificationTurnResponder(
            conversationManager, agentRepository, backgroundTaskService, responseBuilder);
        _suggestionGrounding = new SuggestionGrounding(suggestionEntityNameReader);
        _multiTurnLoop = new MultiTurnChatLoop(
            logger, conversationManager, functionExecutor, recipeEngine, recipeRunRecorder, turnPreparation);
        _streamedTurn = new StreamedChatTurn(
            logger, functionExecutor, responseBuilder, recipeEngine, recipeRunRecorder, turnPreparation,
            turnCompletionRecorder, _suggestionGrounding);
    }

    /// <summary>
    /// Drops any suggestion chip that does not match a real entity name for the given recipe slot; see
    /// SuggestionGrounding. Internal: covered by LLMServiceSuggestionGroundingTests.
    /// </summary>
    internal Task ApplySuggestionGroundingAsync(LLMResponse response, string? slot, CancellationToken cancellationToken = default) =>
        _suggestionGrounding.ApplyAsync(response, slot, cancellationToken);

    public async Task<LLMResponse> ProcessAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId} (conversation {ConversationId}, {MessageLength} chars)",
                context.UserId, context.ConversationId.ForLog(), context.Message.LengthForLog());

            var prepared = await _contextPreparer.PrepareAsync(context);

            if (prepared.Error != null) return _responseBuilder.BuildErrorResponse(prepared.Error);

            var model = prepared.Model;
            var provider = prepared.Provider;
            var conversation = prepared.Conversation;

            if (context.CorrectionClarificationReply is { Length: > 0 } clarification)
            {
                try
                {
                    await _clarificationResponder.PersistAsync(
                        context, conversation!, model!, provider!, clarification, stopwatch, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving the correction clarification for user {UserId}", context.UserId);
                }

                return _clarificationResponder.BuildResponse(conversation!, clarification);
            }

            var totalUsage = new Providers.LLMUsage();
            var ctx = new MultiTurnContext(
                context, model!, provider!, prepared.SystemPrompt!, prepared.History!, totalUsage, conversation!, stopwatch,
                prepared.VolatilePrompt ?? string.Empty, prepared.BudgetProfile, cancellationToken);

            var (responseContent, lastResponse, iterationsUsed, allFunctionCalls, askedSlot) =
                await _multiTurnLoop.ExecuteAsync(ctx);

            if (lastResponse is { Success: false })
            {
                return _responseBuilder.BuildErrorResponse(lastResponse.Error ?? MultiTurnChatLoop.ProviderErrorFallback);
            }

            await _conversationManager.SaveConversationMessagesAsync(
                conversation!, context.Message, responseContent, model!.ModelId);

            await _conversationManager.TrackUsageAsync(
                context.UserId, model, conversation!,
                totalUsage, stopwatch.ElapsedMilliseconds,
                toolsetAssemblyMs: context.ToolsetAssemblyMs, toolIterations: iterationsUsed,
                turnId: context.TurnId, functionsCalledJson: TurnTelemetry.SerializeFunctionsCalled(allFunctionCalls),
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

        yield return SseChunk.Status(SseStatusStages.PreparingContext, TurnTelemetry.ElapsedMsFor(context));

        string? preparationError = null;
        PreparedChatTurn? prepared = null;

        try
        {
            prepared = await _contextPreparer.PrepareAsync(context, cancellationToken);
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

        if (prepared!.Error != null)
        {
            _turnState.TrySetOutcome(TurnOutcome.Errored);
            yield return SseChunk.Error(prepared.Error);
            yield break;
        }

        var conversation = prepared.Conversation!;

        yield return SseChunk.StreamStart(conversation.ConversationId, context.TurnId);

        if (context.CorrectionClarificationReply is { Length: > 0 } streamClarification)
        {
            yield return SseChunk.Content(streamClarification);
            _turnState.TrySetOutcome(TurnOutcome.Clarified);

            try
            {
                await _clarificationResponder.PersistAsync(
                    context, conversation, prepared.Model!, prepared.Provider!, streamClarification, stopwatch, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving the correction clarification for user {UserId}", context.UserId);
            }

            yield return SseChunk.Metadata(_clarificationResponder.BuildResponse(conversation, streamClarification));
            yield return SseChunk.Done();
            yield break;
        }

        await foreach (var turnChunk in _streamedTurn.StreamAsync(_turnState, context, prepared, stopwatch, cancellationToken))
        {
            yield return turnChunk;
        }
    }

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

    /// <summary>
    /// The tool loop of a non-streaming turn; see MultiTurnChatLoop. Internal: the loop tests drive it directly.
    /// </summary>
    internal Task<(string responseContent, LLMProviderResponse? lastResponse, int iterationsUsed, List<LLMFunctionCall> allFunctionCalls, string? askedSlot)> ExecuteMultiTurnLoopAsync(MultiTurnContext ctx) =>
        _multiTurnLoop.ExecuteAsync(ctx);
}
