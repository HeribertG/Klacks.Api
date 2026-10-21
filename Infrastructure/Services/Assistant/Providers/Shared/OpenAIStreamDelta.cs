// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIStreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("function_call")]
    public OpenAIStreamFunctionCall? FunctionCall { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAIStreamToolCall>? ToolCalls { get; set; }
}
