// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of resolving the model and provider a request runs on. On success Model and Provider are set; on
/// failure only Error (English, user-facing summary) and, for a known condition, ErrorCode are set.
/// </summary>
/// <param name="Model">The resolved, enabled model; null when resolution failed.</param>
/// <param name="Provider">The provider serving the model; null when resolution failed.</param>
/// <param name="Error">Why resolution failed; null on success.</param>
/// <param name="ErrorCode">Machine-readable code of the failure (AssistantChatErrorCodes); null when it has none.</param>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record LLMModelResolution(
    LLMModel? Model,
    ILLMProvider? Provider,
    string? Error,
    string? ErrorCode = null);
