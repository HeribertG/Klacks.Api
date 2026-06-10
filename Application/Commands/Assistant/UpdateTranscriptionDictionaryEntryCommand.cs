// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partially updates a transcription dictionary entry. Null properties keep the current
/// value; an empty string clears an optional text field; a non-null variant list replaces
/// the existing phonetic variants.
/// </summary>
/// <param name="Id">Identifier of the entry to update</param>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class UpdateTranscriptionDictionaryEntryCommand : IRequest<TranscriptionDictionaryEntry?>
{
    public Guid Id { get; set; }
    public string? CorrectTerm { get; set; }
    public string? Category { get; set; }
    public List<string>? PhoneticVariants { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
}
