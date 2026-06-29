// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// Uses the knowledge index for semantic retrieval to select relevant skills.
/// </summary>
/// <param name="Message">User's chat message.</param>
/// <param name="UserRights">User's permissions for skill access control.</param>
/// <param name="ModelId">Optional specific LLM model to use.</param>
/// <param name="Language">User's UI language (de, en, fr, it).</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

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
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly RecipeEngineService _recipeEngine;

    private const int MaxToolsForProvider = KnowledgeIndexConstants.MaxToolsForProvider;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentRepository agentRepository,
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        IPlanningScopeEnricher planningScopeEnricher,
        RecipeEngineService recipeEngine)
    {
        _llmService = llmService;
        _agentRepository = agentRepository;
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
        _planningScopeEnricher = planningScopeEnricher;
        _recipeEngine = recipeEngine;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var agent = request.AgentId.HasValue
            ? await _agentRepository.GetByIdAsync(request.AgentId.Value, cancellationToken)
            : await _skillCacheService.GetDefaultAgentAsync(cancellationToken);

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            PageContext = request.PageContext,
            AvailableFunctions = await GetFilteredFunctionsAsync(
                agent, request.UserRights, request.Message, request.PageContext?.CurrentRoute,
                request.UserId, request.ConversationId, request.Language, cancellationToken)
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);

        return await _llmService.ProcessAsync(context);
    }

    private async Task<List<LLMFunction>> GetFilteredFunctionsAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? currentRoute,
        string userId,
        string? conversationId,
        string? language,
        CancellationToken cancellationToken)
    {
        if (agent == null) return [];

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permittedSkills = FilterByPermissions(skills, userRights);

        var alwaysOnSkills = permittedSkills
            .Where(s => s.AlwaysOn)
            .ToList();

        var isAdmin = userRights.Contains(Roles.Admin);
        var retrieval = await _knowledgeRetrieval.RetrieveAsync(
            userMessage, userRights, isAdmin, KnowledgeIndexConstants.DefaultTopK, cancellationToken);

        List<AgentSkill> retrievedSkills;
        if (!retrieval.IsEmpty)
        {
            var retrievedNames = retrieval.Candidates
                .Select(c => c.Entry.SourceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            retrievedSkills = permittedSkills
                .Where(s => !s.AlwaysOn && retrievedNames.Contains(s.Name))
                .ToList();
        }
        else
        {
            retrievedSkills = [];
        }

        // Guarantee the explain skill of the page the user is on and concept explain skills
        // triggered by keywords in the message, independent of retrieval quality
        // (same rationale as in LLMStreamingOrchestrator).
        var guaranteedSkills = new HashSet<AgentSkill>();
        var pageExplainSkill = ResolvePageExplainSkill(permittedSkills, currentRoute);
        if (pageExplainSkill != null)
        {
            guaranteedSkills.Add(pageExplainSkill);
        }

        foreach (var conceptSkillName in ConceptExplainSkillKeywords.ResolveSkillNames(userMessage))
        {
            var conceptSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, conceptSkillName, StringComparison.OrdinalIgnoreCase));
            if (conceptSkill != null)
            {
                guaranteedSkills.Add(conceptSkill);
            }
        }

        // Recipe skill guarantee: when an operator-authored recipe engages, ALL of its step skills must
        // be in the tool set so the forcing spine can narrow the iteration to them (mirrors the streaming
        // orchestrator). Without this, find_customer_candidates is missing and the spine cannot force it.
        foreach (var recipeSkillName in RecipeForcingResolver.GuaranteedSkillNames(userMessage))
        {
            var recipeSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, recipeSkillName, StringComparison.OrdinalIgnoreCase));
            if (recipeSkill != null)
            {
                guaranteedSkills.Add(recipeSkill);
            }
        }

        // Data-driven recipe guarantee (mirrors the streaming orchestrator): step skills of a recipe
        // engaging now or resuming on an ask in this conversation must be in the tool set.
        foreach (var recipeSkillName in await _recipeEngine.GuaranteedSkillNamesAsync(userId, conversationId, userMessage, language, cancellationToken))
        {
            var recipeSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, recipeSkillName, StringComparison.OrdinalIgnoreCase));
            if (recipeSkill != null)
            {
                guaranteedSkills.Add(recipeSkill);
            }
        }

        foreach (var guaranteed in guaranteedSkills)
        {
            if (!guaranteed.AlwaysOn &&
                !retrievedSkills.Any(s => string.Equals(s.Name, guaranteed.Name, StringComparison.OrdinalIgnoreCase)))
            {
                retrievedSkills.Insert(0, guaranteed);
            }
        }

        if (retrievedSkills.Count == 0)
        {
            return alwaysOnSkills.Select(ConvertToLLMFunction).ToList();
        }

        var selectedSkills = alwaysOnSkills
            .Concat(retrievedSkills)
            .DistinctBy(s => s.Name)
            .ToList();

        if (selectedSkills.Count > MaxToolsForProvider)
        {
            selectedSkills = selectedSkills
                .OrderByDescending(s => s.AlwaysOn)
                .ThenByDescending(s => guaranteedSkills.Contains(s))
                .ThenBy(s => s.SortOrder)
                .Take(MaxToolsForProvider)
                .ToList();
        }

        return selectedSkills.Select(ConvertToLLMFunction).ToList();
    }

    private static AgentSkill? ResolvePageExplainSkill(IReadOnlyList<AgentSkill> permittedSkills, string? currentRoute)
    {
        var skillName = PageExplainSkillRoutes.ResolveSkillName(currentRoute);
        if (skillName == null)
        {
            return null;
        }

        return permittedSkills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
    }

    private static List<AgentSkill> FilterByPermissions(IReadOnlyList<AgentSkill> skills, List<string> userRights)
    {
        return skills
            .Where(s => s.RequiredPermission == null ||
                        userRights.Contains(s.RequiredPermission) ||
                        userRights.Contains(Roles.Admin))
            .ToList();
    }

    private static LLMFunction ConvertToLLMFunction(AgentSkill skill)
    {
        var parameters = new Dictionary<string, object>();
        var requiredParameters = new List<string>();

        var paramDefs = JsonSerializer.Deserialize<List<ParameterDefinition>>(
            skill.ParametersJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        foreach (var param in paramDefs)
        {
            var paramDict = new Dictionary<string, object>
            {
                ["type"] = NormalizeToJsonSchemaType(param.Type),
                ["description"] = param.Description
            };

            if (param.EnumValues is { Count: > 0 })
            {
                paramDict["enum"] = param.EnumValues;
            }

            if (param.DefaultValue != null)
            {
                paramDict["default"] = param.DefaultValue;
            }

            parameters[param.Name] = paramDict;

            if (param.Required)
            {
                requiredParameters.Add(param.Name);
            }
        }

        return new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = parameters,
            RequiredParameters = requiredParameters
        };
    }

    private static string NormalizeToJsonSchemaType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "string" => "string",
            "integer" => "integer",
            "decimal" or "number" => "number",
            "boolean" => "boolean",
            "date" or "time" or "datetime" => "string",
            "array" => "array",
            "object" => "object",
            "enum" => "string",
            _ => "string"
        };
    }

    private class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<string>? EnumValues { get; set; }
    }
}
