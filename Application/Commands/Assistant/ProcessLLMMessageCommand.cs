// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// The toolset is built by ISkillToolsetAssembler, shared with the streaming path.
/// </summary>
/// <param name="Message">User's chat message.</param>
/// <param name="UserRights">User's permissions for skill access control.</param>
/// <param name="ModelId">Optional specific LLM model to use.</param>
/// <param name="Language">User's UI language (de, en, fr, it).</param>

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Commands.Assistant;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();
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

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentRepository agentRepository,
        ISkillCacheService skillCacheService,
        ISkillToolsetAssembler toolsetAssembler,
        IPlanningScopeEnricher planningScopeEnricher)
    {
        _llmService = llmService;
        _agentRepository = agentRepository;
        _skillCacheService = skillCacheService;
        _toolsetAssembler = toolsetAssembler;
        _planningScopeEnricher = planningScopeEnricher;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var agent = request.AgentId.HasValue
            ? await _agentRepository.GetByIdAsync(request.AgentId.Value, cancellationToken)
            : await _skillCacheService.GetDefaultAgentAsync(cancellationToken);

        var toolset = await _toolsetAssembler.AssembleAsync(
            agent, request.UserRights, request.Message, request.ConversationId,
            request.PageContext?.CurrentRoute, request.UserId, request.Language, cancellationToken);

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            PageContext = request.PageContext,
            IsVoiceMode = request.IsVoiceMode,
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);

        return await _llmService.ProcessAsync(context);
    }
}
