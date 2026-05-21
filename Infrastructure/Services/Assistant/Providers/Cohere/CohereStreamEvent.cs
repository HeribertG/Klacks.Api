// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;

public class CohereStreamEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<CohereStreamToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_delta")]
    public CohereStreamToolCallDelta? ToolCallDelta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
