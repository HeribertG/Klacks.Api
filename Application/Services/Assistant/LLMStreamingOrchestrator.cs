// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Orchestrates LLM streaming by preparing the context (agent, toolset via ISkillToolsetAssembler)
/// and delegating to ILLMService. Bypasses the Mediator pipeline since it does not support IAsyncEnumerable.
/// Emits a status event before any of that work starts, so the browser can show progress during the
/// seconds the toolset assembly takes, and publishes the turn id as the ambient TurnCorrelation every
/// log line of this turn is joined by. Turn clock and turn id come from the caller when it supplies
/// them - ChatController does, and it announces the same stage even earlier, right after the response
/// head; without them the orchestrator starts its own clock and mints its own id.
/// </summary>
/// <param name="request">Contains message, userId, modelId, language and user rights</param>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.Application.Services.Assistant;

public class LLMStreamingOrchestrator : ILLMStreamingOrchestrator
{
    private readonly ILLMService _llmService;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ICorrectionTurnPreparer _correctionTurnPreparer;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly IExplainDepthResolver _explainDepthResolver;
    private readonly IGateExplainOfferService _gateExplainOffers;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        ISkillCacheService skillCacheService,
        ICorrectionTurnPreparer correctionTurnPreparer,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        LLMProviderOrchestrator providerOrchestrator,
        IContextBudgetPolicy contextBudgetPolicy,
        IExplainDepthResolver explainDepthResolver,
        IGateExplainOfferService gateExplainOffers,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _skillCacheService = skillCacheService;
        _correctionTurnPreparer = correctionTurnPreparer;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _providerOrchestrator = providerOrchestrator;
        _contextBudgetPolicy = contextBudgetPolicy;
        _explainDepthResolver = explainDepthResolver;
        _gateExplainOffers = gateExplainOffers;
        _logger = logger;
    }

    public async IAsyncEnumerable<SseChunk> ProcessStreamAsync(
        LLMStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var turnStartTimestamp = request.TurnStartTimestamp ?? Stopwatch.GetTimestamp();
        var turnId = request.TurnId == Guid.Empty ? Guid.NewGuid() : request.TurnId;

        yield return SseChunk.Status(
            SseStatusStages.AssemblingToolset,
            (long)Stopwatch.GetElapsedTime(turnStartTimestamp).TotalMilliseconds);

        // Set AFTER the yield, not before: everything up to a yield runs in its own resumption, and the
        // consumer restores its own execution context when it comes back, which drops an AsyncLocal
        // written here. This set therefore covers the assembly segment only - the turn's dominant
        // retrieval pass - and is gone again the moment the consumer resumes; both halves of that are
        // asserted by LLMStreamingOrchestratorStatusTests. The carrier for the whole turn is the set in
        // ChatController, which runs in the flow driving this enumeration and passes its id in here.
        TurnCorrelation.Set(turnId);

        Agent? agent = null;
        string? agentLoadError = null;
        try
        {
            agent = await _skillCacheService.GetDefaultAgentAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agent for streaming");
            agentLoadError = "Failed to load agent.";
        }

        if (agentLoadError != null)
        {
            yield return SseChunk.Error(agentLoadError);
            yield break;
        }

        // Resolved early (redundantly re-resolved later by ILLMService itself) so the toolset budget
        // can be sized to the model's real input limit before AssembleAsync runs. request.ModelId is
        // pinned to the model actually resolved here so the later re-resolution inside ILLMService
        // cannot land on a different model (GetDefaultModelAsync is not deterministic across calls).
        var (earlyModel, earlyProvider, _) = await _providerOrchestrator.GetModelAndProviderAsync(request.ModelId);
        var maxToolsForProvider = earlyModel != null && earlyProvider != null
            ? _contextBudgetPolicy.Resolve(earlyProvider, earlyModel).MaxToolsForProvider
            : KnowledgeIndexConstants.MaxToolsForProvider;
        var effectiveModelId = earlyModel?.ModelId ?? request.ModelId;

        var (toolset, correction, undoWasHeld) = await _correctionTurnPreparer.PrepareAsync(
            agent, request.UserRights, request.Message, request.ConversationId, request.UserId,
            request.Language, request.PageContext?.CurrentRoute, maxToolsForProvider, cancellationToken);

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            TurnId = turnId,
            TurnStartTimestamp = turnStartTimestamp,
            StopToken = request.StopToken,
            ModelId = effectiveModelId,
            ProviderId = LLMCapabilityService.MapProvider(earlyModel?.ProviderId),
            Language = request.Language,
            UserRights = request.UserRights,
            AccessToken = request.AccessToken,
            PageContext = request.PageContext,
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext,
            IsVoiceMode = request.IsVoiceMode,
            ToolsetAssemblyMs = toolset.AssemblyMs,
            TurnIntentVetoesForcedToolCall = toolset.TurnIntentVetoesForcedToolCall,
            DraftLoopOpen = toolset.DraftLoopOpen,
            SkillRerank = toolset.SkillRerank,
            CorrectionNote = correction?.ContextNote,
            CorrectionClarificationReply = correction?.ClarificationReply,
            GracefulCorrectionApplied = correction != null,
            CorrectionUndoOffered = undoWasHeld,
            ExplainDepth = await _explainDepthResolver.ResolveAsync(request.UserId, cancellationToken)
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);
        await _gateExplainOffers.PrepareTurnAsync(context, cancellationToken);

        var conversationId = context.ConversationId;
        await foreach (var chunk in _llmService.ProcessStreamAsync(context, cancellationToken))
        {
            if (chunk.Type == SseChunkType.StreamStart && !string.IsNullOrEmpty(chunk.ConversationId))
            {
                conversationId = chunk.ConversationId;
            }
            else if (chunk.Type == SseChunkType.Metadata)
            {
                chunk.SuggestedReplies = await _gateExplainOffers.AugmentRepliesAsync(
                    context, conversationId, chunk.ExecutedCalls, chunk.SuggestedReplies, cancellationToken);
            }

            yield return chunk;
        }
    }
}
