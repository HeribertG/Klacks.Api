// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles ProcessLLMMessageCommand: runs one chat turn against the configured LLM providers with the
/// toolset assembled for the message.
/// </summary>
/// <param name="llmService">Runs the assembled turn context through the model and tool loop</param>
/// <param name="agentRepository">Loads the agent addressed by the command</param>
/// <param name="skillCacheService">Supplies the default agent when the command names none</param>
/// <param name="correctionTurnPreparer">Builds the toolset and detects a correction of the previous turn</param>
/// <param name="planningScopeEnricher">Adds the planning scope to the turn context</param>
/// <param name="entityCandidateGrounder">Grounds entity mentions in the message to candidate records</param>
/// <param name="providerOrchestrator">Resolves the model and provider for the turn</param>
/// <param name="contextBudgetPolicy">Caps the tool count for the resolved model and provider</param>
/// <param name="logger">Receives turn diagnostics</param>

using System.Diagnostics;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ICorrectionTurnPreparer _correctionTurnPreparer;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly ILogger<ProcessLLMMessageCommandHandler> _logger;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentRepository agentRepository,
        ISkillCacheService skillCacheService,
        ICorrectionTurnPreparer correctionTurnPreparer,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        LLMProviderOrchestrator providerOrchestrator,
        IContextBudgetPolicy contextBudgetPolicy,
        ILogger<ProcessLLMMessageCommandHandler> logger)
    {
        _llmService = llmService;
        _agentRepository = agentRepository;
        _skillCacheService = skillCacheService;
        _correctionTurnPreparer = correctionTurnPreparer;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _providerOrchestrator = providerOrchestrator;
        _contextBudgetPolicy = contextBudgetPolicy;
        _logger = logger;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var turnStartTimestamp = Stopwatch.GetTimestamp();
        var turnId = Guid.NewGuid();
        TurnCorrelation.Set(turnId);

        var agent = request.AgentId.HasValue
            ? await _agentRepository.GetByIdAsync(request.AgentId.Value, cancellationToken)
            : await _skillCacheService.GetDefaultAgentAsync(cancellationToken);

        // Resolved early (redundantly re-resolved later by ILLMService itself) so the toolset budget
        // can be sized to the model's real input limit before AssembleAsync runs. context.ModelId is
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
            ModelId = effectiveModelId,
            ProviderId = LLMCapabilityService.MapProvider(earlyModel?.ProviderId),
            Language = request.Language,
            UserRights = request.UserRights,
            AccessToken = request.AccessToken,
            PageContext = request.PageContext,
            IsVoiceMode = request.IsVoiceMode,
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext,
            ToolsetAssemblyMs = toolset.AssemblyMs,
            CorrectionNote = correction?.ContextNote,
            CorrectionClarificationReply = correction?.ClarificationReply,
            GracefulCorrectionApplied = correction != null,
            CorrectionUndoOffered = undoWasHeld
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);

        return await _llmService.ProcessAsync(context, cancellationToken);
    }
}
