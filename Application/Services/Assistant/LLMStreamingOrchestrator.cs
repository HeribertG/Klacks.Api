// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates LLM streaming by preparing the context (agent, skill filtering via knowledge index) and delegating to ILLMService.
/// Bypasses the Mediator pipeline since it does not support IAsyncEnumerable.
/// </summary>
/// <param name="request">Contains message, userId, modelId, language and user rights</param>

using System.Runtime.CompilerServices;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

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
    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    private const int MaxToolsForProvider = 30;

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
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

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, ct);
        var permittedSkills = skills
            .Where(s => s.RequiredPermission == null ||
                        userRights.Contains(s.RequiredPermission) ||
                        userRights.Contains(Roles.Admin))
            .ToList();

        var alwaysOnSkills = permittedSkills.Where(s => s.AlwaysOn).ToList();

        var isAdmin = userRights.Contains(Roles.Admin);
        var retrieval = await _knowledgeRetrieval.RetrieveAsync(
            userMessage, userRights, isAdmin, KnowledgeIndexConstants.DefaultTopK, ct);

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

        if (retrievedSkills.Count == 0)
            return alwaysOnSkills.Select(ConvertToLLMFunction).ToList();

        var selectedSkills = alwaysOnSkills.Concat(retrievedSkills).DistinctBy(s => s.Name).ToList();

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
