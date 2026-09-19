// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIThinkingOptions
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
