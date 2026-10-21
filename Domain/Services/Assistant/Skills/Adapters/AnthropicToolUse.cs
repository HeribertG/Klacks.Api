// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

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
