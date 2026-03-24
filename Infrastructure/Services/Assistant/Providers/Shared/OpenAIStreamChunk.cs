// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Models for deserializing OpenAI-compatible streaming response chunks.
/// @param Choices - List of completion choices with delta content
/// @param Usage - Token usage info (only present in final chunk with stream_options)
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIStreamChunk
{
    [JsonPropertyName("choices")]
    public List<OpenAIStreamChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public OpenAIUsageResponse? Usage { get; set; }
}

public class OpenAIStreamChoice
{
    [JsonPropertyName("delta")]
    public OpenAIStreamDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class OpenAIStreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("function_call")]
    public OpenAIStreamFunctionCall? FunctionCall { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAIStreamToolCall>? ToolCalls { get; set; }
}

public class OpenAIStreamFunctionCall
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}

public class OpenAIStreamToolCall
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public OpenAIStreamFunctionCall? Function { get; set; }
}

public class OpenAIUsageResponse
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
