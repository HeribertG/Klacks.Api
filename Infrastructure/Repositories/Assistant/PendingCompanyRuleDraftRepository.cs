// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IPendingCompanyRuleDraftRepository. UpsertAsync enforces a single row per
/// (UserId, ConversationId) by updating the existing row in place or inserting a fresh one; DeleteAsync
/// and PruneExpiredAsync issue hard deletes because a draft row is ephemeral and must vanish once the
/// rule is applied, abandoned or expires.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class PendingCompanyRuleDraftRepository : IPendingCompanyRuleDraftRepository
{
    private readonly DataBaseContext _context;

    public PendingCompanyRuleDraftRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PendingCompanyRuleDraftRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingCompanyRuleDrafts
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ConversationId == conversationId, cancellationToken);
    }

    public async Task UpsertAsync(PendingCompanyRuleDraftRow row, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(row.UserId, row.ConversationId, cancellationToken);
        if (existing == null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            await _context.PendingCompanyRuleDrafts.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.DraftJson = row.DraftJson;
        existing.ExpiresAtUtc = row.ExpiresAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.PendingCompanyRuleDrafts
            .Where(p => p.UserId == userId && p.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _context.PendingCompanyRuleDrafts.RemoveRange(rows);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var expired = await _context.PendingCompanyRuleDrafts
            .Where(p => p.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        _context.PendingCompanyRuleDrafts.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
