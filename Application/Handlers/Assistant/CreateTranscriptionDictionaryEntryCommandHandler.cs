// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that creates a transcription dictionary entry and invalidates the dictionary cache
/// so the next transcription pass picks up the new term.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
/// <param name="dictionaryService">Dictionary service whose cache is invalidated after the write</param>
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CreateTranscriptionDictionaryEntryCommandHandler
    : IRequestHandler<CreateTranscriptionDictionaryEntryCommand, TranscriptionDictionaryEntry>
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IDictionaryService _dictionaryService;

    public CreateTranscriptionDictionaryEntryCommandHandler(
        ITranscriptionDictionaryRepository repository,
        IDictionaryService dictionaryService)
    {
        _repository = repository;
        _dictionaryService = dictionaryService;
    }

    public async Task<TranscriptionDictionaryEntry> Handle(
        CreateTranscriptionDictionaryEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = new TranscriptionDictionaryEntry
        {
            Id = Guid.NewGuid(),
            CorrectTerm = request.CorrectTerm,
            Category = request.Category,
            PhoneticVariants = request.PhoneticVariants,
            Description = request.Description,
            Language = request.Language
        };

        await _repository.AddAsync(entry, cancellationToken);
        _dictionaryService.InvalidateCache();

        return entry;
    }
}
