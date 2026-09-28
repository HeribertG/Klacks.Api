// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Represents a model discovered from a provider's /models API endpoint.
/// </summary>
/// <param name="ApiModelId">The model identifier used in API calls</param>
/// <param name="ModelName">Human-readable display name from the provider</param>
namespace Klacks.Api.Domain.Models.Assistant;

public record LLMModelDiscovery(string ApiModelId, string ModelName);
