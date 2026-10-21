// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// OpenAI-compatible request using the modern "tools" format (vs legacy "functions" in OpenAIRequest).
/// Used by DeepSeek, Mistral, Generic and other OpenAI-compatible providers.
/// Temperature is nullable so providers can omit it for models that reject the parameter; a null value
/// leaves the field out of the payload. DeepSeek and the generic backends always assign a value, so
/// their serialized behaviour is unchanged.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIToolsRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxTokens { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAITool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }

    /// <summary>
    /// Asks the API to append a final chunk carrying the token counters, which a stream otherwise
    /// never reports. Null omits the field, because not every OpenAI-compatible endpoint accepts it.
    /// </summary>
    [JsonPropertyName("stream_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIStreamOptions? StreamOptions { get; set; }

    [JsonPropertyName("thinking")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIThinkingOptions? Thinking { get; set; }

    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Stop { get; set; }
}
