// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Base class for LLM provider skill adapters: converts skill descriptors into provider tool
/// formats and parses provider function calls back into skill invocations.
/// </summary>
/// <param name="descriptor">Skill definition converted into the provider-specific tool format</param>
/// <param name="providerFunctionCall">Raw provider function call parsed into a skill invocation</param>

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public abstract class BaseSkillAdapter : ISkillAdapter
{
    public abstract LLMProviderType ProviderType { get; }

    public abstract object ConvertSkillToProviderFormat(SkillDescriptor descriptor);

    public abstract SkillInvocation ParseProviderCall(object providerFunctionCall);

    public virtual object ConvertResultToProviderFormat(SkillResult result)
    {
        return new
        {
            success = result.Success,
            data = result.Data,
            message = result.Message
        };
    }

    protected static Dictionary<string, object> ConvertParameterToJsonSchema(SkillParameter param)
    {
        return SkillParameterSchemaBuilder.BuildParameterSchema(param);
    }

    protected static Dictionary<string, object> ParseArgumentsJson(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
