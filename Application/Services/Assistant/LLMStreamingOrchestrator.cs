// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates LLM streaming by preparing the context (agent, toolset via ISkillToolsetAssembler)
/// and delegating to ILLMService. Bypasses the Mediator pipeline since it does not support IAsyncEnumerable.
/// </summary>
/// <param name="request">Contains message, userId, modelId, language and user rights</param>

using System.Runtime.CompilerServices;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

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
    public AssistantPageContext? PageContext { get; set; }
    public bool IsVoiceMode { get; set; }
}

public class LLMStreamingOrchestrator : ILLMStreamingOrchestrator
{
    private readonly ILLMService _llmService;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        ISkillCacheService skillCacheService,
        ISkillToolsetAssembler toolsetAssembler,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _skillCacheService = skillCacheService;
        _toolsetAssembler = toolsetAssembler;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _logger = logger;
    }

    public async IAsyncEnumerable<SseChunk> ProcessStreamAsync(
        LLMStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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

        SkillToolsetResult toolset;
        try
        {
            toolset = await _toolsetAssembler.AssembleAsync(
                agent, request.UserRights, request.Message, request.ConversationId,
                request.PageContext?.CurrentRoute, request.UserId, request.Language, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assembling skill toolset for streaming");
            toolset = new SkillToolsetResult();
        }

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            PageContext = request.PageContext,
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext,
            IsVoiceMode = request.IsVoiceMode
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);

        await foreach (var chunk in _llmService.ProcessStreamAsync(context, cancellationToken))
        {
            yield return chunk;
        }
    }
}
