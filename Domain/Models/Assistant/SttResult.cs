// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of a speech-to-text transcription chunk.
/// </summary>
/// <param name="Text">Transcribed text</param>
/// <param name="IsFinal">Whether this is a final or interim result</param>
/// <param name="Confidence">Confidence score 0.0-1.0</param>
namespace Klacks.Api.Domain.Models.Assistant;

public record SttResult(string Text, bool IsFinal, float Confidence);
