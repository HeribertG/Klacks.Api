// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IRecentEntityRepository. Add inserts a row and then hard-deletes any rows
/// beyond <see cref="RecentEntityDefaults.MaxPerConversation"/> newest rows for that (user, conversation),
/// keeping the per-conversation ring bounded; GetRecent returns the retained rows ordered newest-first.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class RecentEntityRepository : IRecentEntityRepository
{
    private readonly DataBaseContext _context;

    public RecentEntityRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RecentEntityRow row, CancellationToken cancellationToken = default)
    {
        row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
        if (row.CreatedAtUtc == default)
        {
            row.CreatedAtUtc = DateTime.UtcNow;
        }

        await _context.RecentEntities.AddAsync(row, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var overflow = await _context.RecentEntities
            .Where(r => r.UserId == row.UserId && r.ConversationId == row.ConversationId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip(RecentEntityDefaults.MaxPerConversation)
            .ToListAsync(cancellationToken);

        if (overflow.Count > 0)
        {
            _context.RecentEntities.RemoveRange(overflow);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<RecentEntityRow>> GetRecentAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.RecentEntities
            .Where(r => r.UserId == userId && r.ConversationId == conversationId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Take(RecentEntityDefaults.MaxPerConversation)
            .ToListAsync(cancellationToken);
    }
}
