// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json;
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
