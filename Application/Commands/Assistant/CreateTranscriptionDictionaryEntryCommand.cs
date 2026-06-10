// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class CreateTranscriptionDictionaryEntryCommand : IRequest<TranscriptionDictionaryEntry>
{
    public string CorrectTerm { get; set; } = string.Empty;
    public string? Category { get; set; }
    public List<string> PhoneticVariants { get; set; } = [];
    public string? Description { get; set; }
    public string? Language { get; set; }
}
