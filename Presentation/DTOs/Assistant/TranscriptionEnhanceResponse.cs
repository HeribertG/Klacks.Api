// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Response containing the LLM-enhanced transcription text.
/// </summary>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public class TranscriptionEnhanceResponse
{
    public string EnhancedText { get; set; } = string.Empty;
}
