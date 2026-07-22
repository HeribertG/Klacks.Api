// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IPendingConfirmationRepository. ConsumeAsync tracks the row by token,
/// removes it and saves; a DbUpdateConcurrencyException (0 rows affected because another call already
/// deleted the same row) means the race was lost, so the caller gets null instead of the exception —
/// this is the DB-level guard against a double redemption of the same one-time token. GetActiveForUserAsync
/// and PruneExpiredAsync issue plain hard deletes because a confirmation row is ephemeral and must vanish
/// once consumed, mismatched or expired.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class PendingConfirmationRepository : IPendingConfirmationRepository
{
    private readonly DataBaseContext _context;

    public PendingConfirmationRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PendingConfirmationRow row, CancellationToken cancellationToken = default)
    {
        row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
        await _context.PendingConfirmations.AddAsync(row, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PendingConfirmationRow?> ConsumeAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await _context.PendingConfirmations
            .FirstOrDefaultAsync(p => p.Token == token, cancellationToken);
        if (row == null)
        {
            return null;
        }

        _context.PendingConfirmations.Remove(row);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return row;
    }

    public async Task<IReadOnlyList<PendingConfirmationRow>> GetActiveForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.PendingConfirmations
            .Where(p => p.UserId == userId && p.ExpiresAtUtc >= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var expired = await _context.PendingConfirmations
            .Where(p => p.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        _context.PendingConfirmations.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
