// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

public interface ILLMStreamingOrchestrator
{
    IAsyncEnumerable<SseChunk> ProcessStreamAsync(LLMStreamRequest request, CancellationToken cancellationToken = default);
}

public class LLMStreamRequest
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();

    public BearerToken? AccessToken { get; set; }
    public AssistantPageContext? PageContext { get; set; }
    public bool IsVoiceMode { get; set; }

    /// <summary>
    /// Id this turn is logged and stored under. Supplied by the caller so it can publish the same id
    /// as the ambient TurnCorrelation from ITS own async flow: an AsyncLocal written inside an async
    /// iterator is not guaranteed to survive a yield back to the consumer, so the value has to be set
    /// where the enumeration is driven from. Empty means "generate one here".
    /// </summary>
    public Guid TurnId { get; set; }

    /// <summary>
    /// Stopwatch timestamp the turn's elapsed times are measured from. Supplied by the caller so the
    /// status events count from when the request arrived rather than from when this orchestrator is
    /// reached - the caller has already normalized, matched and logged by then. Null means "start the
    /// clock here".
    /// </summary>
    public long? TurnStartTimestamp { get; set; }
}

public class LLMStreamingOrchestrator : ILLMStreamingOrchestrator
{
    private readonly ILLMService _llmService;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        ISkillCacheService skillCacheService,
        ISkillToolsetAssembler toolsetAssembler,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        LLMProviderOrchestrator providerOrchestrator,
        IContextBudgetPolicy contextBudgetPolicy,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _skillCacheService = skillCacheService;
        _toolsetAssembler = toolsetAssembler;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _providerOrchestrator = providerOrchestrator;
        _contextBudgetPolicy = contextBudgetPolicy;
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

        SkillToolsetResult toolset;
        try
        {
            toolset = await _toolsetAssembler.AssembleAsync(
                agent, request.UserRights, request.Message, request.ConversationId,
                request.PageContext?.CurrentRoute, request.UserId, request.Language,
                maxToolsForProvider, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Error assembling skill toolset for streaming - this turn runs with ZERO tools, so the assistant " +
                "cannot perform any action and may answer as if it had. User {UserId}, conversation {ConversationId}",
                request.UserId, request.ConversationId);
            toolset = new SkillToolsetResult();
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
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext,
            IsVoiceMode = request.IsVoiceMode,
            ToolsetAssemblyMs = toolset.AssemblyMs
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);

        await foreach (var chunk in _llmService.ProcessStreamAsync(context, cancellationToken))
        {
            yield return chunk;
        }
    }
}
