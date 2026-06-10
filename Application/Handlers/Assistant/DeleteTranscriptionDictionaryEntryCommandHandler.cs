// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that deletes a transcription dictionary entry and invalidates the dictionary cache.
/// Returns false when the entry does not exist.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
/// <param name="dictionaryService">Dictionary service whose cache is invalidated after the delete</param>
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeleteTranscriptionDictionaryEntryCommandHandler
    : IRequestHandler<DeleteTranscriptionDictionaryEntryCommand, bool>
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IDictionaryService _dictionaryService;

    public DeleteTranscriptionDictionaryEntryCommandHandler(
        ITranscriptionDictionaryRepository repository,
        IDictionaryService dictionaryService)
    {
        _repository = repository;
        _dictionaryService = dictionaryService;
    }

    public async Task<bool> Handle(
        DeleteTranscriptionDictionaryEntryCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        await _repository.DeleteAsync(request.Id, cancellationToken);
        _dictionaryService.InvalidateCache();

        return true;
    }
}
