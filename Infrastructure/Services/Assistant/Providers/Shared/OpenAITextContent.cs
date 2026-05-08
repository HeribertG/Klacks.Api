// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

/// <summary>
/// OpenAI multimodal content block of type <c>text</c>. Used inside <see cref="OpenAIMessage.Content"/>
/// when the same message also carries an image block; vision-capable models read both blocks.
/// </summary>
/// <param name="Text">Text payload of this block.</param>
public sealed record OpenAITextContent(
    [property: JsonPropertyName("text")] string Text)
{
    [JsonPropertyName("type")]
    public string Type => "text";
}
