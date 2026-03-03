// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Skills.Adapters;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

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
        var descriptors = _skillRegistry.GetSkillsForUser(userPermissions);

        return descriptors.Select(descriptor => new LLMFunction
        {
            Name = descriptor.Name,
            Description = descriptor.Description,
            Parameters = descriptor.Parameters.ToDictionary(
                p => p.Name,
                p => (object)ConvertParameterToLLMFormat(p)),
            RequiredParameters = descriptor.Parameters
                .Where(p => p.Required)
                .Select(p => p.Name)
                .ToList()
        }).ToList();
    }

    public async Task<SkillBridgeResult> ExecuteSkillFromLLMCallAsync(
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

        return new SkillBridgeResult
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
