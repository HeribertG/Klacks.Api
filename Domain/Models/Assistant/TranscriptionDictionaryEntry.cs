// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A dictionary entry for transcription enhancement with correct spelling and phonetic variants.
/// </summary>
/// <param name="CorrectTerm">The correct spelling of the term</param>
/// <param name="PhoneticVariants">Common misrecognitions by STT engines</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class TranscriptionDictionaryEntry : BaseEntity
{
    public string CorrectTerm { get; set; } = string.Empty;
    public string? Category { get; set; }
    public List<string> PhoneticVariants { get; set; } = [];
    public string? Description { get; set; }
    public string? Language { get; set; }
}
