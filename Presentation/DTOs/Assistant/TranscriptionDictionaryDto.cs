// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Data transfer object for a transcription dictionary entry.
/// </summary>
/// <param name="Id">Unique entry identifier</param>
/// <param name="CorrectTerm">The correct spelling of the term</param>
/// <param name="Category">Optional category grouping</param>
/// <param name="PhoneticVariants">Common misrecognitions by STT engines</param>
/// <param name="Description">Optional description of the term</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record TranscriptionDictionaryDto(
    Guid Id,
    string CorrectTerm,
    string? Category,
    List<string> PhoneticVariants,
    string? Description);
