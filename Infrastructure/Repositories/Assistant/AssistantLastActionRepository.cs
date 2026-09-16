// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAssistantLastActionRepository. Upsert enforces a single row per
/// (UserId, ConversationId) by updating the existing row in place or inserting a fresh one; PruneExpired
/// issues hard deletes because the record is ephemeral. Self-committing, like the sibling assistant
/// repositories: the callers are the chat pipeline's store, which has no unit of work. The read-then-
/// insert in Upsert is not protected by a lock - the unique index on (UserId, ConversationId) is the
/// actual arbiter of two turns of one conversation racing, and a unique-violation on the insert makes
/// Upsert retry as an update of the row the other writer just created.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AssistantLastActionRepository : IAssistantLastActionRepository
{
    private const string UniqueViolationSqlState = "23505";

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

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                _context.Entry(row).State = EntityState.Detached;
                existing = await GetAsync(row.UserId, row.ConversationId, cancellationToken);
                if (existing == null)
                {
                    throw;
                }
            }
        }

        ApplyFields(existing, row);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyFields(AssistantLastActionRow existing, AssistantLastActionRow row)
    {
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
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;

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
