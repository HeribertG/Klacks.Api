// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response containing the LLM-enhanced transcription text.
/// </summary>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public class TranscriptionEnhanceResponse
{
    public string EnhancedText { get; set; } = string.Empty;
}
