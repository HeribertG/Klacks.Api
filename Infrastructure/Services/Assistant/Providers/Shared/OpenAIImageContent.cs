// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

/// <summary>
/// OpenAI multimodal content block of type <c>image_url</c>. The <see cref="ImageUrl"/> may be a
/// data URI (<c>data:image/png;base64,...</c>) for inline images or a remote URL. Used in
/// <see cref="OpenAIMessage.Content"/> alongside <see cref="OpenAITextContent"/> for vision calls.
/// </summary>
/// <param name="ImageUrl">Wrapper carrying the actual <c>url</c> field expected by the API.</param>
public sealed record OpenAIImageContent(
    [property: JsonPropertyName("image_url")] OpenAIImageUrl ImageUrl)
{
    [JsonPropertyName("type")]
    public string Type => "image_url";
}

/// <param name="Url">Either a remote https URL or an inline <c>data:image/png;base64,...</c> URI.</param>
public sealed record OpenAIImageUrl(
    [property: JsonPropertyName("url")] string Url);
