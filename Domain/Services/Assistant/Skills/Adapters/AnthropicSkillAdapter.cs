// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public class AnthropicSkillAdapter : BaseSkillAdapter
{
    public override LLMProviderType ProviderType => LLMProviderType.Anthropic;

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

        return new AnthropicToolDefinition
        {
            Name = descriptor.Name,
            Description = descriptor.Description,
            InputSchema = new AnthropicInputSchemaDefinition
            {
                Type = "object",
                Properties = properties,
                Required = required
            }
        };
    }

    public override SkillInvocation ParseProviderCall(object providerFunctionCall)
    {
        if (providerFunctionCall is JsonElement jsonElement)
        {
            var name = jsonElement.GetProperty("name").GetString() ?? "";
            var input = jsonElement.GetProperty("input");
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(input.GetRawText())
                             ?? new Dictionary<string, object>();

            return new SkillInvocation
            {
                SkillName = name,
                Parameters = parameters
            };
        }

        if (providerFunctionCall is AnthropicToolUse toolUse)
        {
            return new SkillInvocation
            {
                SkillName = toolUse.Name,
                Parameters = toolUse.Input ?? new Dictionary<string, object>()
            };
        }

        throw new ArgumentException($"Unsupported provider call type: {providerFunctionCall.GetType().Name}");
    }
}
