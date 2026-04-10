// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for enhancing raw speech-to-text transcriptions using LLM processing.
/// </summary>
/// <param name="rawText">The raw, unprocessed speech-to-text output</param>
/// <param name="locale">The language locale of the transcription (e.g. "de", "en")</param>
/// <param name="modelId">Optional model ID override; when null falls back to the configured setting</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITranscriptionEnhancerService
{
    Task<string> EnhanceTranscriptionAsync(string rawText, string locale, string? modelId = null, CancellationToken ct = default);
}
