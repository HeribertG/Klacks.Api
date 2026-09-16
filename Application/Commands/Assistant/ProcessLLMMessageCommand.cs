// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// The toolset is built by ISkillToolsetAssembler, shared with the streaming path.
/// </summary>
/// <param name="Message">User's chat message.</param>
/// <param name="UserRights">User's permissions for skill access control.</param>
/// <param name="ModelId">Optional specific LLM model to use.</param>
/// <param name="Language">User's UI language (de, en, fr, it).</param>

using System.Diagnostics;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.Application.Commands.Assistant;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();

    public BearerToken? AccessToken { get; set; }
    public Guid? AgentId { get; set; }
    public AssistantPageContext? PageContext { get; set; }
    public bool IsVoiceMode { get; set; }
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly IAssistantLastActionStore _lastActionStore;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ITurnPreparationService _turnPreparation;

    /// <summary>
    /// Holds the one-time token of an undo offer. Written HERE and in LLMStreamingOrchestrator only,
    /// never in the turn preparation: a headless replay resolves the same undo as data and must leave no
    /// redeemable token behind. The token carries PendingConfirmationPurposes.GateReplay because it is
    /// redeemed exactly like any other held invocation - an affirmation narrows the next turn to
    /// confirm_pending_action, which replays these arguments. Known limitation, not introduced here:
    /// PeekLatestForUser looks up the latest token PER USER, not per conversation, so an affirmation in
    /// another conversation of the same user that is open at the same time can redeem this one. TP1
    /// narrows the window (the token exists only on the non-ambiguous path and only when an offer was
    /// actually made); conversation-scoped confirmations are TP2 work.
    /// </summary>
    private readonly IPendingConfirmationStore _pendingConfirmationStore;

    private readonly ILogger<ProcessLLMMessageCommandHandler> _logger;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentRepository agentRepository,
        ISkillCacheService skillCacheService,
        ISkillToolsetAssembler toolsetAssembler,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        LLMProviderOrchestrator providerOrchestrator,
        IContextBudgetPolicy contextBudgetPolicy,
        IAssistantLastActionStore lastActionStore,
        IPendingRecipeStore pendingRecipeStore,
        ITurnPreparationService turnPreparation,
        IPendingConfirmationStore pendingConfirmationStore,
        ILogger<ProcessLLMMessageCommandHandler> logger)
    {
        _llmService = llmService;
        _agentRepository = agentRepository;
        _skillCacheService = skillCacheService;
        _toolsetAssembler = toolsetAssembler;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _providerOrchestrator = providerOrchestrator;
        _contextBudgetPolicy = contextBudgetPolicy;
        _lastActionStore = lastActionStore;
        _pendingRecipeStore = pendingRecipeStore;
        _turnPreparation = turnPreparation;
        _pendingConfirmationStore = pendingConfirmationStore;
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

        Guid.TryParse(request.UserId, out var userGuid);
        var hasConversation = userGuid != Guid.Empty && !string.IsNullOrEmpty(request.ConversationId);

        AssistantLastAction? lastAction = null;
        GracefulCorrectionPlan? correctionPlan = null;
        try
        {
            if (hasConversation)
            {
                lastAction = _lastActionStore.Peek(userGuid, request.ConversationId!);
            }

            var recipeIsActive = lastAction?.CanAnchorCorrection(DateTime.UtcNow) == true
                && _pendingRecipeStore.Peek(userGuid, request.ConversationId!) != null;

            correctionPlan = await _turnPreparation.PlanCorrectionAsync(
                new GracefulCorrectionInput(
                    agent, request.UserRights, request.Message, request.ConversationId, request.UserId,
                    request.Language, lastAction, recipeIsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Graceful correction planning failed for user {UserId}; continuing as an ordinary turn.",
                request.UserId);
        }

        var toolset = await _toolsetAssembler.AssembleAsync(
            agent, request.UserRights, correctionPlan?.CompositeMessage ?? request.Message,
            request.ConversationId, request.PageContext?.CurrentRoute, request.UserId, request.Language,
            maxToolsForProvider, applyLearnedPhraseGuarantee: true,
            excludedSkillNames: correctionPlan?.ExcludedSkillNames,
            pinnedSkillNames: lastAction?.ClarificationSkillNames,
            cancellationToken: cancellationToken);

        GracefulCorrectionOutcome? correction = null;
        if (correctionPlan != null)
        {
            try
            {
                correction = _turnPreparation.CompleteCorrection(
                    correctionPlan, toolset.Functions, request.Language);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Completing the graceful correction failed for user {UserId}; continuing as an ordinary turn.",
                    request.UserId);
            }
        }

        if (correction is { ClarificationReply.Length: > 0, ClarificationSkillNames.Count: > 0 } && hasConversation)
        {
            try
            {
                _lastActionStore.SaveClarificationCandidates(
                    userGuid, request.ConversationId!, correction.ClarificationSkillNames);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Could not pin the clarification candidates for user {UserId}; the follow-up turn runs without them.",
                    request.UserId);
            }
        }

        if (correction?.Undo != null && userGuid != Guid.Empty)
        {
            try
            {
                _pendingConfirmationStore.Create(
                    userGuid,
                    correction.Undo.SkillName,
                    correction.Undo.Arguments,
                    PendingConfirmationPurposes.CorrectionUndo);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Could not register the undo offer for skill {Skill}", correction.Undo.SkillName);
            }
        }

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
            GracefulCorrectionApplied = correction != null
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);

        return await _llmService.ProcessAsync(context, cancellationToken);
    }
}
