// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        var schema = new Dictionary<string, object>
        {
            ["description"] = param.Description
        };

        schema["type"] = param.Type switch
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
        };

        if (param.Type == SkillParameterType.Date)
        {
            schema["format"] = "date";
        }
        else if (param.Type == SkillParameterType.Time)
        {
            schema["pattern"] = "^\\d{2}:\\d{2}$";
        }
        else if (param.Type == SkillParameterType.DateTime)
        {
            schema["format"] = "date-time";
        }

        if (param.EnumValues is { Count: > 0 })
        {
            schema["enum"] = param.EnumValues;
        }

        if (param.DefaultValue != null)
        {
            schema["default"] = param.DefaultValue;
        }

        return schema;
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
