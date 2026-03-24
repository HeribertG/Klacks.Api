// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates LLM streaming by preparing the context (agent, skill filtering) and delegating to ILLMService.
/// Bypasses the Mediator pipeline since it does not support IAsyncEnumerable.
/// </summary>
/// <param name="request">Contains message, userId, modelId, language and user rights</param>

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
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
}

public class LLMStreamingOrchestrator : ILLMStreamingOrchestrator
{
    private readonly ILLMService _llmService;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillClassifierService _skillClassifierService;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    private const int MaxToolsForProvider = 30;
    private const int WordBoundaryThreshold = 5;
    private const int MinMessageLengthForClassification = 20;

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ISkillClassifierService skillClassifierService,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillClassifierService = skillClassifierService;
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
            agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
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

        List<LLMFunction> functions;
        try
        {
            functions = await GetFilteredFunctionsAsync(
                agent, request.UserRights, request.Message, request.Language, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering functions for streaming");
            functions = new List<LLMFunction>();
        }

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            AvailableFunctions = functions
        };

        await foreach (var chunk in _llmService.ProcessStreamAsync(context, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task<List<LLMFunction>> GetFilteredFunctionsAsync(
        Agent? agent, List<string> userRights, string userMessage, string? language, CancellationToken ct)
    {
        if (agent == null) return [];

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id, ct);
        var permittedSkills = skills
            .Where(s => s.RequiredPermission == null ||
                        userRights.Contains(s.RequiredPermission) ||
                        userRights.Contains(Roles.Admin))
            .ToList();

        var alwaysOnSkills = permittedSkills.Where(s => s.AlwaysOn).ToList();
        var keywordMatchedSkills = permittedSkills
            .Where(s => !s.AlwaysOn && MatchesSkillKeywords(s, userMessage, language))
            .ToList();

        if (keywordMatchedSkills.Count == 0 && userMessage.Length > MinMessageLengthForClassification)
        {
            var classifiedKeywords = await _skillClassifierService.ClassifyMessageAsync(userMessage, language, ct);
            if (classifiedKeywords.Count > 0)
            {
                var classifiedMessage = string.Join(" ", classifiedKeywords);
                keywordMatchedSkills = permittedSkills
                    .Where(s => !s.AlwaysOn && MatchesSkillKeywords(s, classifiedMessage, language))
                    .ToList();
            }
        }

        if (keywordMatchedSkills.Count == 0)
            return alwaysOnSkills.Select(ConvertToLLMFunction).ToList();

        var selectedSkills = alwaysOnSkills.Concat(keywordMatchedSkills).DistinctBy(s => s.Name).ToList();

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
        if (MatchesSynonyms(skill.Synonyms, messageLower, language)) return true;
        return MatchesLegacyTriggerKeywords(skill.TriggerKeywords, messageLower);
    }

    private static bool MatchesSynonyms(Dictionary<string, List<string>>? synonyms, string messageLower, string? language)
    {
        if (synonyms == null || synonyms.Count == 0) return false;
        var langs = GetLanguagePriority(language);
        foreach (var lang in langs)
        {
            if (!synonyms.TryGetValue(lang, out var keywords) || keywords.Count == 0) continue;
            if (keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant()))) return true;
        }
        return false;
    }

    private static bool MatchesLegacyTriggerKeywords(string triggerKeywords, string messageLower)
    {
        if (string.IsNullOrWhiteSpace(triggerKeywords) || triggerKeywords == "[]") return false;
        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(triggerKeywords);
            if (keywords == null || keywords.Count == 0) return false;
            return keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant()));
        }
        catch { return false; }
    }

    private static bool MatchesKeyword(string message, string keyword)
    {
        if (keyword.Length < WordBoundaryThreshold)
            return Regex.IsMatch(message, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);
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

    private static LLMFunction ConvertToLLMFunction(AgentSkill skill)
    {
        var parameters = new Dictionary<string, object>();
        var requiredParameters = new List<string>();
        var paramDefs = JsonSerializer.Deserialize<List<ParameterDefinition>>(
            skill.ParametersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        foreach (var param in paramDefs)
        {
            var paramDict = new Dictionary<string, object>
            {
                ["type"] = NormalizeToJsonSchemaType(param.Type),
                ["description"] = param.Description
            };
            if (param.EnumValues is { Count: > 0 }) paramDict["enum"] = param.EnumValues;
            if (param.DefaultValue != null) paramDict["default"] = param.DefaultValue;
            parameters[param.Name] = paramDict;
            if (param.Required) requiredParameters.Add(param.Name);
        }

        return new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = parameters,
            RequiredParameters = requiredParameters
        };
    }

    private static string NormalizeToJsonSchemaType(string type) => type.ToLowerInvariant() switch
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
