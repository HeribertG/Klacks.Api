// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Models for deserializing OpenAI-compatible streaming response chunks.
/// @param Choices - List of completion choices with delta content
/// @param Usage - Token usage info (only present in final chunk with stream_options)
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIStreamChunk
{
    [JsonPropertyName("choices")]
    public List<OpenAIStreamChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public OpenAIUsageResponse? Usage { get; set; }
}
