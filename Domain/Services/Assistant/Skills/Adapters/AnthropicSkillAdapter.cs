using System.Text.Json;
using System.Text.Json.Serialization;
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

public class AnthropicToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public AnthropicInputSchemaDefinition InputSchema { get; set; } = new();
}

public class AnthropicInputSchemaDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class AnthropicToolUse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "tool_use";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public Dictionary<string, object>? Input { get; set; }
}
