// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request to test an STT provider connection with a given provider identifier.
/// </summary>
/// <param name="ProviderId">Provider identifier (e.g. "deepgram")</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record SttTestRequest(string ProviderId);
