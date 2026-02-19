using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public class GeminiSkillAdapter : BaseSkillAdapter
{
    public override LLMProviderType ProviderType => LLMProviderType.Google;

    public override object ConvertSkillToProviderFormat(SkillDescriptor descriptor)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in descriptor.Parameters)
        {
            properties[param.Name] = ConvertParameterToGeminiSchema(param);

            if (param.Required)
            {
                required.Add(param.Name);
            }
        }

        return new GeminiFunctionDeclarationDefinition
        {
            Name = descriptor.Name,
            Description = descriptor.Description,
            Parameters = new GeminiParametersDefinition
            {
                Type = "OBJECT",
                Properties = properties,
                Required = required.Count > 0 ? required : null
            }
        };
    }

    public override SkillInvocation ParseProviderCall(object providerFunctionCall)
    {
        if (providerFunctionCall is JsonElement jsonElement)
        {
            var name = jsonElement.GetProperty("name").GetString() ?? "";
            var args = jsonElement.GetProperty("args");
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText())
                             ?? new Dictionary<string, object>();

            return new SkillInvocation
            {
                SkillName = name,
                Parameters = parameters
            };
        }

        if (providerFunctionCall is GeminiFunctionCall functionCall)
        {
            return new SkillInvocation
            {
                SkillName = functionCall.Name,
                Parameters = functionCall.Args ?? new Dictionary<string, object>()
            };
        }

        throw new ArgumentException($"Unsupported provider call type: {providerFunctionCall.GetType().Name}");
    }

    private static Dictionary<string, object> ConvertParameterToGeminiSchema(SkillParameter param)
    {
        var schema = new Dictionary<string, object>
        {
            ["description"] = param.Description
        };

        schema["type"] = param.Type switch
        {
            SkillParameterType.String => "STRING",
            SkillParameterType.Integer => "INTEGER",
            SkillParameterType.Decimal => "NUMBER",
            SkillParameterType.Boolean => "BOOLEAN",
            SkillParameterType.Date => "STRING",
            SkillParameterType.Time => "STRING",
            SkillParameterType.DateTime => "STRING",
            SkillParameterType.Array => "ARRAY",
            SkillParameterType.Object => "OBJECT",
            SkillParameterType.Enum => "STRING",
            _ => "STRING"
        };

        if (param.EnumValues is { Count: > 0 })
        {
            schema["enum"] = param.EnumValues;
        }

        return schema;
    }
}

public class GeminiFunctionDeclarationDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiParametersDefinition? Parameters { get; set; }
}

public class GeminiParametersDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "OBJECT";

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }
}

public class GeminiFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }
}
