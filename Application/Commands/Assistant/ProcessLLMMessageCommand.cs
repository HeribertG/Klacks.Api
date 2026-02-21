using System.Text.Json;
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
            AvailableFunctions = await GetAvailableFunctionsAsync(agent, request.UserRights, cancellationToken)
        };

        return await _llmService.ProcessAsync(context);
    }

    private async Task<List<LLMFunction>> GetAvailableFunctionsAsync(Agent? agent, List<string> userRights, CancellationToken cancellationToken)
    {
        if (agent == null) return [];

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id, cancellationToken);
        var functions = new List<LLMFunction>();

        foreach (var skill in skills)
        {
            if (skill.RequiredPermission == null ||
                userRights.Contains(skill.RequiredPermission) ||
                userRights.Contains("Admin"))
            {
                functions.Add(ConvertToLLMFunction(skill));
            }
        }

        return functions;
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

            if (param.Enum is { Count: > 0 })
            {
                paramDict["enum"] = param.Enum.ToArray();
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
        public List<string>? Enum { get; set; }
    }
}
