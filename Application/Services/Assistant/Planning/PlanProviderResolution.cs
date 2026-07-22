// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of resolving the LLM provider used to attribute a plan execution. HasDefaultModel is
/// false when no default LLM model is configured (callers surface a 409 / skill error); ProviderId
/// is the mapped provider of that default model, or null when the model exists but is unmapped.
/// </summary>
/// <param name="HasDefaultModel">Whether a default LLM model is configured.</param>
/// <param name="ProviderId">Mapped provider of the default model, for usage attribution; null when unmapped.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public sealed record PlanProviderResolution(bool HasDefaultModel, LLMProviderType? ProviderId);
