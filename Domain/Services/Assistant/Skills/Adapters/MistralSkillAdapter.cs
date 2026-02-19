using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public class MistralSkillAdapter : BaseSkillAdapter
{
    public override LLMProviderType ProviderType => LLMProviderType.Mistral;

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

        return new MistralToolDefinition
        {
            Type = "function",
            Function = new MistralFunctionDefinition
            {
                Name = descriptor.Name,
                Description = descriptor.Description,
                Parameters = new MistralParametersDefinition
                {
                    Type = "object",
                    Properties = properties,
                    Required = required.Count > 0 ? required : null
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

        if (providerFunctionCall is MistralToolCall toolCall)
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

public class MistralToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public MistralFunctionDefinition Function { get; set; } = new();
}

public class MistralFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public MistralParametersDefinition Parameters { get; set; } = new();
}

public class MistralParametersDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }
}

public class MistralToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public MistralFunctionCallDetails Function { get; set; } = new();
}

public class MistralFunctionCallDetails
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
