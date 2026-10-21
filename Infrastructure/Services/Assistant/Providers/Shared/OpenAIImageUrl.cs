// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <param name="Url">Either a remote https URL or an inline <c>data:image/png;base64,...</c> URI.</param>
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public sealed record OpenAIImageUrl(
    [property: JsonPropertyName("url")] string Url);
