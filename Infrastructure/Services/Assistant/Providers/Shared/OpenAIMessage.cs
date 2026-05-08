// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Polymorph: a plain <see cref="string"/> for text-only messages, or an array of content
    /// blocks (e.g. <see cref="OpenAITextContent"/> + <see cref="OpenAIImageContent"/>) when the
    /// message carries image data. The OpenAI Chat Completions API accepts both shapes on the
    /// same field — vision-capable models read content blocks, text-only models the string.
    /// </summary>
    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;

    [JsonPropertyName("function_call")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIFunctionCall? FunctionCall { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAIToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Reads <see cref="Content"/> as a flat string regardless of whether it was assigned a
    /// raw <see cref="string"/> or deserialized into a <see cref="JsonElement"/>. The OpenAI
    /// API responds with a string while we send arrays in multimodal requests, so the same
    /// property accepts both shapes; this helper hides the difference from the response path.
    /// </summary>
    public string GetContentString()
    {
        return Content switch
        {
            string s => s,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonElement element => element.GetRawText(),
            null => string.Empty,
            _ => Content.ToString() ?? string.Empty,
        };
    }
}

public class OpenAIToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunctionCall Function { get; set; } = new();
}