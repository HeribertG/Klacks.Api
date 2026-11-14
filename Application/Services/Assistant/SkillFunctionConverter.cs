// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns an agent skill into the function definition the model sees: name, description, JSON-schema parameters
/// from the skill's parameter definitions, labels, effect and, when known, where the skill came from in the
/// toolset. Shared by the toolset assembly and the tool search, so a skill reaches the model in one shape only.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillFunctionConverter
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <param name="skill">The skill to describe to the model</param>
    /// <param name="source">How the skill entered the toolset; null leaves the provenance empty</param>
    /// <param name="retrievalScore">The retrieval score that surfaced the skill, if any</param>
    public static LLMFunction ToFunction(AgentSkill skill, ToolsetSkillSource? source = null, double? retrievalScore = null)
    {
        var parameters = new Dictionary<string, object>();
        var requiredParameters = new List<string>();
        var paramDefs = JsonSerializer.Deserialize<List<ParameterDefinition>>(
            skill.ParametersJson, CaseInsensitiveJsonOptions) ?? [];

        foreach (var param in paramDefs)
        {
            var paramDict = new Dictionary<string, object>
            {
                ["type"] = NormalizeToJsonSchemaType(param.Type),
                ["description"] = param.Description
            };
            if (param.EnumValues is { Count: > 0 }) paramDict[SkillParameterSchemaKeys.Enum] = param.EnumValues;
            if (param.DefaultValue != null) paramDict["default"] = param.DefaultValue;
            parameters[param.Name] = paramDict;
            if (param.Required) requiredParameters.Add(param.Name);
        }

        var function = new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = parameters,
            RequiredParameters = requiredParameters,
            Labels = skill.Labels,
            Effect = skill.Effect
        };

        if (source != null)
        {
            function.ToolsetSource = source;
            function.RetrievalScore = retrievalScore;
        }

        return function;
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

    private sealed class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<string>? EnumValues { get; set; }
    }
}
