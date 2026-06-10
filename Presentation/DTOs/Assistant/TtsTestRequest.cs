// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request to test a TTS provider connection with a given provider identifier.
/// </summary>
/// <param name="ProviderId">Provider identifier (e.g. "openai")</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record TtsTestRequest(string ProviderId);
