// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IPendingRecipeRepository. Upsert enforces a single row per
/// (UserId, ConversationId) by updating the existing row in place or inserting a fresh one; Delete and
/// PruneExpired issue hard deletes because a paused-recipe row is ephemeral and must vanish once the
/// flow completes, is abandoned or expires.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class PendingRecipeRepository : IPendingRecipeRepository
{
    private readonly DataBaseContext _context;

    public PendingRecipeRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PendingRecipeRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingRecipes
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ConversationId == conversationId, cancellationToken);
    }

    public async Task UpsertAsync(PendingRecipeRow row, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(row.UserId, row.ConversationId, cancellationToken);
        if (existing == null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            await _context.PendingRecipes.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.RecipeName = row.RecipeName;
        existing.StepIndex = row.StepIndex;
        existing.SlotsJson = row.SlotsJson;
        existing.ExpiresAtUtc = row.ExpiresAtUtc;
        existing.AwaitingConfirmation = row.AwaitingConfirmation;
        existing.CaptureRewindUsed = row.CaptureRewindUsed;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.PendingRecipes
            .Where(p => p.UserId == userId && p.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _context.PendingRecipes.RemoveRange(rows);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var expired = await _context.PendingRecipes
            .Where(p => p.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        _context.PendingRecipes.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
