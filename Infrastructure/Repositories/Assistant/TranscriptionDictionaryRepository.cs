// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for transcription dictionary CRUD with soft-delete support.
/// </summary>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class TranscriptionDictionaryRepository : ITranscriptionDictionaryRepository
{
    private readonly DataBaseContext _context;

    public TranscriptionDictionaryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<TranscriptionDictionaryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionDictionaryEntries
            .OrderBy(e => e.Category)
            .ThenBy(e => e.CorrectTerm)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TranscriptionDictionaryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionDictionaryEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(TranscriptionDictionaryEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.TranscriptionDictionaryEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TranscriptionDictionaryEntry entry, CancellationToken cancellationToken = default)
    {
        _context.TranscriptionDictionaryEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.TranscriptionDictionaryEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry != null)
        {
            _context.TranscriptionDictionaryEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
