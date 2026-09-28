// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

/// <summary>
/// Text content block inside an Anthropic Messages API multimodal message.
/// </summary>
/// <param name="Text">Plain text content shown to the model.</param>
public sealed class AnthropicTextBlock
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}
