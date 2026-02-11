using System.Text.Json;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.AI;
using Klacks.Api.Domain.Services.LLM;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly ILlmFunctionDefinitionRepository _functionDefinitionRepository;

    public ProcessLLMMessageCommandHandler(
        ILLMService llmService,
        ILlmFunctionDefinitionRepository functionDefinitionRepository)
    {
        _llmService = llmService;
        _functionDefinitionRepository = functionDefinitionRepository;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            AvailableFunctions = await GetAvailableFunctionsAsync(request.UserRights, cancellationToken)
        };

        return await _llmService.ProcessAsync(context);
    }

    private async Task<List<LLMFunction>> GetAvailableFunctionsAsync(List<string> userRights, CancellationToken cancellationToken)
    {
        var definitions = await _functionDefinitionRepository.GetAllEnabledAsync(cancellationToken);
        var functions = new List<LLMFunction>();

        foreach (var def in definitions)
        {
            if (def.RequiredPermission == null ||
                userRights.Contains(def.RequiredPermission) ||
                userRights.Contains("Admin"))
            {
                functions.Add(ConvertToLLMFunction(def));
            }
        }

        return functions;
    }

    private static LLMFunction ConvertToLLMFunction(LlmFunctionDefinition definition)
    {
        var parameters = new Dictionary<string, object>();
        var requiredParameters = new List<string>();

        var paramDefs = JsonSerializer.Deserialize<List<ParameterDefinition>>(
            definition.ParametersJson,
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
            Name = definition.Name,
            Description = definition.Description,
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
