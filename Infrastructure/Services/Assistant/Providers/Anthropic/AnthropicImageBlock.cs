// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

/// <summary>
/// Image content block inside an Anthropic Messages API multimodal message. The image is
/// transported as base64-encoded bytes with an explicit media type; the Anthropic backend
/// rejects raw URLs without the wrapper.
/// </summary>
/// <param name="Source">Base64 source descriptor with media type and data.</param>
public sealed class AnthropicImageBlock
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "image";

    [JsonPropertyName("source")]
    public AnthropicImageSource Source { get; init; } = new();
}

public sealed class AnthropicImageSource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; init; } = "image/png";

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;
}
