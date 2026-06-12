// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds JSON schema dictionaries from skill definitions, shared by LLM provider adapters and the MCP tool bridge.
/// </summary>
/// <param name="descriptor">Skill whose parameters are converted into a JSON schema object</param>
/// <param name="param">Single skill parameter translated into JSON schema fields (type, format, enum, default)</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public static class SkillParameterSchemaBuilder
{
    private const string TypeKey = "type";
    private const string PropertiesKey = "properties";
    private const string RequiredKey = "required";
    private const string DescriptionKey = "description";
    private const string FormatKey = "format";
    private const string PatternKey = "pattern";
    private const string EnumKey = "enum";
    private const string DefaultKey = "default";
    private const string ObjectTypeName = "object";
    private const string DateFormatName = "date";
    private const string DateTimeFormatName = "date-time";
    private const string TimePattern = "^\\d{2}:\\d{2}$";

    public static Dictionary<string, object> BuildInputSchema(SkillDescriptor descriptor)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in descriptor.Parameters)
        {
            properties[param.Name] = BuildParameterSchema(param);

            if (param.Required)
            {
                required.Add(param.Name);
            }
        }

        return new Dictionary<string, object>
        {
            [TypeKey] = ObjectTypeName,
            [PropertiesKey] = properties,
            [RequiredKey] = required
        };
    }

    public static Dictionary<string, object> BuildParameterSchema(SkillParameter param)
    {
        var schema = new Dictionary<string, object>
        {
            [DescriptionKey] = param.Description
        };

        schema[TypeKey] = param.Type switch
        {
            SkillParameterType.String => "string",
            SkillParameterType.Integer => "integer",
            SkillParameterType.Decimal => "number",
            SkillParameterType.Boolean => "boolean",
            SkillParameterType.Date => "string",
            SkillParameterType.Time => "string",
            SkillParameterType.DateTime => "string",
            SkillParameterType.Array => "array",
            SkillParameterType.Object => ObjectTypeName,
            SkillParameterType.Enum => "string",
            _ => "string"
        };

        if (param.Type == SkillParameterType.Date)
        {
            schema[FormatKey] = DateFormatName;
        }
        else if (param.Type == SkillParameterType.Time)
        {
            schema[PatternKey] = TimePattern;
        }
        else if (param.Type == SkillParameterType.DateTime)
        {
            schema[FormatKey] = DateTimeFormatName;
        }

        if (param.EnumValues is { Count: > 0 })
        {
            schema[EnumKey] = param.EnumValues;
        }

        if (param.DefaultValue != null)
        {
            schema[DefaultKey] = param.DefaultValue;
        }

        return schema;
    }
}
