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
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

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
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;

    private const int MaxToolsForProvider = 30;
    private const int WordBoundaryThreshold = 5;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository)
    {
        _llmService = llmService;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var agent = request.AgentId.HasValue
            ? await _agentRepository.GetByIdAsync(request.AgentId.Value, cancellationToken)
            : await _agentRepository.GetDefaultAgentAsync(cancellationToken);

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

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id, cancellationToken);
        var permittedSkills = FilterByPermissions(skills, userRights);

        var alwaysOnSkills = permittedSkills
            .Where(s => s.AlwaysOn)
            .ToList();

        var keywordMatchedSkills = permittedSkills
            .Where(s => !s.AlwaysOn && MatchesSkillKeywords(s, userMessage, language))
            .ToList();

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

    private static bool MatchesSkillKeywords(AgentSkill skill, string userMessage, string? language)
    {
        var messageLower = userMessage.ToLowerInvariant();

        if (MatchesSynonyms(skill.Synonyms, messageLower, language))
            return true;

        return MatchesLegacyTriggerKeywords(skill.TriggerKeywords, messageLower);
    }

    private static bool MatchesSynonyms(
        Dictionary<string, List<string>>? synonyms,
        string messageLower,
        string? language)
    {
        if (synonyms == null || synonyms.Count == 0)
            return false;

        var languagesToCheck = GetLanguagePriority(language);

        foreach (var lang in languagesToCheck)
        {
            if (!synonyms.TryGetValue(lang, out var keywords) || keywords.Count == 0)
                continue;

            if (keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant())))
                return true;
        }

        return false;
    }

    private static bool MatchesLegacyTriggerKeywords(string triggerKeywords, string messageLower)
    {
        if (string.IsNullOrWhiteSpace(triggerKeywords) || triggerKeywords == "[]")
            return false;

        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(triggerKeywords);
            if (keywords == null || keywords.Count == 0) return false;

            return keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesKeyword(string message, string keyword)
    {
        if (keyword.Length < WordBoundaryThreshold)
        {
            return Regex.IsMatch(message, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);
        }

        return message.Contains(keyword);
    }

    private static List<string> GetLanguagePriority(string? language)
    {
        var lang = (language ?? "de").ToLowerInvariant();

        return lang switch
        {
            "de" => ["de", "en"],
            "en" => ["en", "de"],
            "fr" => ["fr", "en", "de"],
            "it" => ["it", "en", "de"],
            _ => [lang, "en", "de"]
        };
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
