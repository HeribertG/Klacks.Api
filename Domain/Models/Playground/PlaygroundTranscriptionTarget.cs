// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The provider and provider-side model the Playground transcription cleanup is pinned to.
/// </summary>
/// <param name="ProviderId">llm_providers.provider_id of the provider that serves the call</param>
/// <param name="ApiModelId">Model id as the provider API expects it</param>

namespace Klacks.Api.Domain.Models.Playground;

public sealed record PlaygroundTranscriptionTarget(string ProviderId, string ApiModelId);
