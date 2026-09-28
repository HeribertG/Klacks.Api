// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request to enhance raw speech-to-text output via LLM-based cleanup.
/// </summary>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public class TranscriptionEnhanceRequest
{
    public string RawText { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string? ModelId { get; set; }
}
