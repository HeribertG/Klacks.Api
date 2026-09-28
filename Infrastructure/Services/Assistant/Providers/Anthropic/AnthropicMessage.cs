// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

/// <summary>
/// Anthropic Messages API message envelope. Content is polymorphic: a plain string
/// for text-only messages or a list of content blocks (text + image) for multimodal
/// requests. Concrete block types live in <see cref="AnthropicTextBlock"/> and
/// <see cref="AnthropicImageBlock"/>.
/// </summary>
public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;
}