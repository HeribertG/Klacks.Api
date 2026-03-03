// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public class OpenAISkillAdapter : BaseSkillAdapter
{
    public override LLMProviderType ProviderType => LLMProviderType.OpenAI;

    public override object ConvertSkillToProviderFormat(SkillDescriptor descriptor)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in descriptor.Parameters)
        {
            properties[param.Name] = ConvertParameterToJsonSchema(param);

            if (param.Required)
            {
                required.Add(param.Name);
            }
        }

        return new OpenAIToolDefinition
        {
            Type = "function",
            Function = new OpenAIFunctionDefinition
            {
                Name = descriptor.Name,
                Description = descriptor.Description,
                Parameters = new OpenAIParametersDefinition
                {
                    Type = "object",
                    Properties = properties,
                    Required = required
                }
            }
        };
    }

    public override SkillInvocation ParseProviderCall(object providerFunctionCall)
    {
        if (providerFunctionCall is JsonElement jsonElement)
        {
            var name = jsonElement.GetProperty("function").GetProperty("name").GetString() ?? "";
            var arguments = jsonElement.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";

            return new SkillInvocation
            {
                SkillName = name,
                Parameters = ParseArgumentsJson(arguments)
            };
        }

        if (providerFunctionCall is OpenAIToolCall toolCall)
        {
            return new SkillInvocation
            {
                SkillName = toolCall.Function.Name,
                Parameters = ParseArgumentsJson(toolCall.Function.Arguments)
            };
        }

        throw new ArgumentException($"Unsupported provider call type: {providerFunctionCall.GetType().Name}");
    }
}

public class OpenAIToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunctionDefinition Function { get; set; } = new();
}

public class OpenAIFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public OpenAIParametersDefinition Parameters { get; set; } = new();
}

public class OpenAIParametersDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class OpenAIToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunctionCallDetails Function { get; set; } = new();
}

public class OpenAIFunctionCallDetails
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
