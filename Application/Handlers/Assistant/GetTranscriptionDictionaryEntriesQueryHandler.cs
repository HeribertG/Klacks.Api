// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that returns all transcription dictionary entries from the repository.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetTranscriptionDictionaryEntriesQueryHandler
    : IRequestHandler<GetTranscriptionDictionaryEntriesQuery, List<TranscriptionDictionaryEntry>>
{
    private readonly ITranscriptionDictionaryRepository _repository;

    public GetTranscriptionDictionaryEntriesQueryHandler(ITranscriptionDictionaryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TranscriptionDictionaryEntry>> Handle(
        GetTranscriptionDictionaryEntriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}
