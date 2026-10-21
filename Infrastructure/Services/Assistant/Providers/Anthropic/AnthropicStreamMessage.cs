// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

internal sealed class AnthropicStreamMessage
{
    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}
