// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// Uses language-specific synonyms (JSONB) and word-boundary matching for precise skill selection.
/// @param Message - User's chat message
/// @param UserRights - User's permissions for skill access control
/// @param ModelId - Optional specific LLM model to use
/// @param Language - User's UI language (de, en, fr, it) for synonym matching
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Application.Services.Assistant;

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
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillClassifierService _skillClassifierService;
    private readonly ISynonymLearningService _synonymLearningService;

    private const int MaxToolsForProvider = 30;
    private const int MinMessageLengthForClassification = 20;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentRepository agentRepository,
        ISkillCacheService skillCacheService,
        ISkillClassifierService skillClassifierService,
        ISynonymLearningService synonymLearningService)
    {
        _llmService = llmService;
        _agentRepository = agentRepository;
        _skillCacheService = skillCacheService;
        _skillClassifierService = skillClassifierService;
        _synonymLearningService = synonymLearningService;
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
            AvailableFunctions = await GetFilteredFunctionsAsync(
                agent, request.UserRights, request.Message, request.Language, cancellationToken)
        };

        return await _llmService.ProcessAsync(context);
    }

    private async Task<List<LLMFunction>> GetFilteredFunctionsAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? language,
        CancellationToken cancellationToken)
    {
        if (agent == null) return [];

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permittedSkills = FilterByPermissions(skills, userRights);

        var alwaysOnSkills = permittedSkills
            .Where(s => s.AlwaysOn)
            .ToList();

        var keywordMatchedSkills = permittedSkills
            .Where(s => !s.AlwaysOn && SkillMatchingEngine.MatchesSkillKeywords(s, userMessage, language))
            .ToList();

        if (keywordMatchedSkills.Count == 0 && userMessage.Length > MinMessageLengthForClassification)
        {
            var classifiedKeywords = await _skillClassifierService.ClassifyMessageAsync(userMessage, language, cancellationToken);

            if (classifiedKeywords.Count > 0)
            {
                var classifiedMessage = string.Join(" ", classifiedKeywords);
                keywordMatchedSkills = permittedSkills
                    .Where(s => !s.AlwaysOn && SkillMatchingEngine.MatchesSkillKeywords(s, classifiedMessage, language))
                    .ToList();

                if (keywordMatchedSkills.Count > 0)
                {
                    var matchedSkillNames = keywordMatchedSkills.Select(s => s.Name).ToList();
                    _synonymLearningService.LearnFromClassifierResult(userMessage, matchedSkillNames, language, agent.Id);
                }
            }
        }

        if (keywordMatchedSkills.Count == 0)
        {
            return alwaysOnSkills.Select(ConvertToLLMFunction).ToList();
        }

        var selectedSkills = alwaysOnSkills
            .Concat(keywordMatchedSkills)
            .DistinctBy(s => s.Name)
            .ToList();

        if (selectedSkills.Count > MaxToolsForProvider)
        {
            selectedSkills = selectedSkills
                .OrderByDescending(s => s.AlwaysOn)
                .ThenBy(s => s.SortOrder)
                .Take(MaxToolsForProvider)
                .ToList();
        }

        return selectedSkills.Select(ConvertToLLMFunction).ToList();
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
