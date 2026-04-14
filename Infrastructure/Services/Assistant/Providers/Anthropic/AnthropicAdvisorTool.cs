// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Server-side advisor tool that allows the executor model to consult a stronger model.
/// </summary>
/// <param name="Model">The advisor model ID (e.g. claude-opus-4-6)</param>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicAdvisorTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "advisor_20260301";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "advisor";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-opus-4-6";
}
