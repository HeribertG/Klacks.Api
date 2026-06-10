// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that partially updates a transcription dictionary entry and invalidates the
/// dictionary cache. Null command properties keep the current value, empty strings clear
/// optional text fields, and a non-null variant list replaces the existing variants.
/// Returns null when the entry does not exist.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
/// <param name="dictionaryService">Dictionary service whose cache is invalidated after the write</param>
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateTranscriptionDictionaryEntryCommandHandler
    : IRequestHandler<UpdateTranscriptionDictionaryEntryCommand, TranscriptionDictionaryEntry?>
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IDictionaryService _dictionaryService;

    public UpdateTranscriptionDictionaryEntryCommandHandler(
        ITranscriptionDictionaryRepository repository,
        IDictionaryService dictionaryService)
    {
        _repository = repository;
        _dictionaryService = dictionaryService;
    }

    public async Task<TranscriptionDictionaryEntry?> Handle(
        UpdateTranscriptionDictionaryEntryCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrectTerm))
        {
            existing.CorrectTerm = request.CorrectTerm;
        }

        if (request.Category != null)
        {
            existing.Category = NullIfEmpty(request.Category);
        }

        if (request.PhoneticVariants != null)
        {
            existing.PhoneticVariants = request.PhoneticVariants;
        }

        if (request.Description != null)
        {
            existing.Description = NullIfEmpty(request.Description);
        }

        if (request.Language != null)
        {
            existing.Language = NullIfEmpty(request.Language);
        }

        await _repository.UpdateAsync(existing, cancellationToken);
        _dictionaryService.InvalidateCache();

        return existing;
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
