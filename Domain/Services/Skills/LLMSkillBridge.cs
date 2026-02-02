using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Domain.Services.Skills.Adapters;
using Klacks.Api.Presentation.DTOs.LLM;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Skills;

public interface ILLMSkillBridge
{
    IReadOnlyList<LLMFunction> GetSkillsAsLLMFunctions(IReadOnlyList<string> userPermissions);

    Task<LLMFunctionResult> ExecuteSkillFromLLMCallAsync(
        LLMFunctionCall functionCall,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);

    IReadOnlyList<object> GetSkillsForProvider(
        LLMProviderType providerType,
        IReadOnlyList<string> userPermissions);
}

public class LLMSkillBridge : ILLMSkillBridge
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ISkillAdapterFactory _adapterFactory;
    private readonly ILogger<LLMSkillBridge> _logger;

    public LLMSkillBridge(
        ISkillRegistry skillRegistry,
        ISkillExecutor skillExecutor,
        ISkillAdapterFactory adapterFactory,
        ILogger<LLMSkillBridge> logger)
    {
        _skillRegistry = skillRegistry;
        _skillExecutor = skillExecutor;
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public IReadOnlyList<LLMFunction> GetSkillsAsLLMFunctions(IReadOnlyList<string> userPermissions)
    {
        var skills = _skillRegistry.GetSkillsForUser(userPermissions);

        return skills.Select(skill => new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = skill.Parameters.ToDictionary(
                p => p.Name,
                p => (object)ConvertParameterToLLMFormat(p)),
            RequiredParameters = skill.Parameters
                .Where(p => p.Required)
                .Select(p => p.Name)
                .ToList()
        }).ToList();
    }

    public async Task<LLMFunctionResult> ExecuteSkillFromLLMCallAsync(
        LLMFunctionCall functionCall,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing skill from LLM call: {SkillName}", functionCall.FunctionName);

        var invocation = new SkillInvocation
        {
            SkillName = functionCall.FunctionName,
            Parameters = functionCall.Parameters ?? new Dictionary<string, object>()
        };

        var result = await _skillExecutor.ExecuteAsync(invocation, context, cancellationToken);

        return new LLMFunctionResult
        {
            Success = result.Success,
            Message = result.Message ?? "",
            Data = result.Data,
            ResultType = result.Type.ToString()
        };
    }

    public IReadOnlyList<object> GetSkillsForProvider(
        LLMProviderType providerType,
        IReadOnlyList<string> userPermissions)
    {
        return _skillRegistry.ExportAsProviderFormat(providerType, userPermissions);
    }

    private static Dictionary<string, object> ConvertParameterToLLMFormat(SkillParameter param)
    {
        var result = new Dictionary<string, object>
        {
            ["type"] = param.Type switch
            {
                SkillParameterType.String => "string",
                SkillParameterType.Integer => "integer",
                SkillParameterType.Decimal => "number",
                SkillParameterType.Boolean => "boolean",
                SkillParameterType.Date => "string",
                SkillParameterType.Time => "string",
                SkillParameterType.DateTime => "string",
                SkillParameterType.Array => "array",
                SkillParameterType.Object => "object",
                SkillParameterType.Enum => "string",
                _ => "string"
            },
            ["description"] = param.Description
        };

        if (param.EnumValues is { Count: > 0 })
        {
            result["enum"] = param.EnumValues;
        }

        if (param.DefaultValue != null)
        {
            result["default"] = param.DefaultValue;
        }

        return result;
    }
}

public class LLMFunctionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string ResultType { get; set; } = "Data";
}
