// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for managing transcription dictionary entries.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITranscriptionDictionaryRepository
{
    Task<List<TranscriptionDictionaryEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TranscriptionDictionaryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TranscriptionDictionaryEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(TranscriptionDictionaryEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
