// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAssistantLastActionRepository. Upsert enforces a single row per
/// (UserId, ConversationId) by updating the existing row in place or inserting a fresh one; PruneExpired
/// issues hard deletes because the record is ephemeral. Self-committing, like the sibling assistant
/// repositories: the callers are the chat pipeline's store, which has no unit of work.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AssistantLastActionRepository : IAssistantLastActionRepository
{
    private readonly DataBaseContext _context;

    public AssistantLastActionRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AssistantLastActionRow?> GetAsync(
        Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.AssistantLastActions
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ConversationId == conversationId, cancellationToken);
    }

    public async Task UpsertAsync(AssistantLastActionRow row, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(row.UserId, row.ConversationId, cancellationToken);
        if (existing == null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            await _context.AssistantLastActions.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Explicit field copy for the same reason PendingRecipeRepository does it: the update path is the
        // normal case (one write per tool-calling turn), so a column added to the row without being added
        // here would be silently dropped on every turn after the first.
        existing.UserMessage = row.UserMessage;
        existing.CallsJson = row.CallsJson;
        existing.AssistantAnswerExcerpt = row.AssistantAnswerExcerpt;
        existing.ClarificationSkillsJson = row.ClarificationSkillsJson;
        existing.CreateTimeUtc = row.CreateTimeUtc;
        existing.SupersededAtUtc = row.SupersededAtUtc;
        existing.ExpiresAtUtc = row.ExpiresAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var expired = await _context.AssistantLastActions
            .Where(r => r.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        _context.AssistantLastActions.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
