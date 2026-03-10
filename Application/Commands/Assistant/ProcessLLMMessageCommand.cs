// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// @param Message - User's chat message
/// @param UserRights - User's permissions for skill access control
/// @param ModelId - Optional specific LLM model to use
/// </summary>

using System.Text.Json;
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
            AvailableFunctions = await GetFilteredFunctionsAsync(agent, request.UserRights, request.Message, cancellationToken)
        };

        return await _llmService.ProcessAsync(context);
    }

    private async Task<List<LLMFunction>> GetFilteredFunctionsAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        CancellationToken cancellationToken)
    {
        if (agent == null) return [];

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id, cancellationToken);
        var permittedSkills = FilterByPermissions(skills, userRights);

        var alwaysOnSkills = permittedSkills
            .Where(s => s.AlwaysOn)
            .ToList();

        var keywordMatchedSkills = permittedSkills
            .Where(s => !s.AlwaysOn && MatchesTriggerKeywords(s, userMessage))
            .ToList();

        var selectedSkills = alwaysOnSkills
            .Concat(keywordMatchedSkills)
            .DistinctBy(s => s.Name)
            .ToList();

        if (selectedSkills.Count < MaxToolsForProvider && keywordMatchedSkills.Count == 0)
        {
            selectedSkills = permittedSkills
                .OrderByDescending(s => s.AlwaysOn)
                .ThenBy(s => s.SortOrder)
                .Take(MaxToolsForProvider)
                .ToList();
        }

        if (selectedSkills.Count > MaxToolsForProvider)
        {
            selectedSkills = selectedSkills
                .OrderByDescending(s => s.AlwaysOn)
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

    private static bool MatchesTriggerKeywords(AgentSkill skill, string userMessage)
    {
        if (string.IsNullOrWhiteSpace(skill.TriggerKeywords) || skill.TriggerKeywords == "[]")
            return false;

        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(skill.TriggerKeywords);
            if (keywords == null || keywords.Count == 0) return false;

            var messageLower = userMessage.ToLowerInvariant();
            return keywords.Any(kw => messageLower.Contains(kw.ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
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
                ["type"] = param.Type,
                ["description"] = param.Description
            };

            if (param.EnumValues is { Count: > 0 })
            {
                paramDict["enum"] = param.EnumValues.ToArray();
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

    private class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public List<string>? EnumValues { get; set; }
    }
}
