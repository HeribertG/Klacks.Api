// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIStreamFunctionCall
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}
