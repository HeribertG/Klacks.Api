// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public sealed class AnthropicImageSource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; init; } = "image/png";

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;
}
