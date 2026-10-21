// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIStreamOptions
{
    [JsonPropertyName("include_usage")]
    public bool IncludeUsage { get; set; }
}
