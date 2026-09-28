// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request to test a TTS provider connection with a given provider identifier.
/// </summary>
/// <param name="ProviderId">Provider identifier (e.g. "openai")</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record TtsTestRequest(string ProviderId);
