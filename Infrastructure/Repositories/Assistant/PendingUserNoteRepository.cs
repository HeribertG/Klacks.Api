// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for PendingUserNote: the agent's persistent scratchpad of information
/// stashed for a user until it is relayed. Delivered notes are soft-deleted (IsDeleted)
/// and remain readable via IgnoreQueryFilters until the data retention purge removes them.
/// </summary>
/// <param name="context">Database context with the PendingUserNotes DbSet</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class PendingUserNoteRepository : IPendingUserNoteRepository
{
    private readonly DataBaseContext _context;

    public PendingUserNoteRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<PendingUserNote>> GetPendingAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingUserNotes
            .Where(n => n.AgentId == agentId && (n.UserId == userId || n.UserId == null))
            .OrderBy(n => n.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PendingUserNote>> GetDeliveredAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingUserNotes
            .IgnoreQueryFilters()
            .Where(n => n.AgentId == agentId && (n.UserId == userId || n.UserId == null) && n.IsDeleted)
            .OrderByDescending(n => n.DeletedTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPendingAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingUserNotes
            .CountAsync(n => n.AgentId == agentId && (n.UserId == userId || n.UserId == null), cancellationToken);
    }

    public async Task<int> ExpireBroadcastsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;

        var expired = await _context.PendingUserNotes
            .Where(n => n.UserId == null && n.FirstDeliveredAt != null && n.FirstDeliveredAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return 0;
        }

        _context.PendingUserNotes.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);

        return expired.Count;
    }

    public async Task<PendingUserNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PendingUserNotes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task AddAsync(PendingUserNote note, CancellationToken cancellationToken = default)
    {
        await _context.PendingUserNotes.AddAsync(note, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> MarkDeliveredAsync(Guid agentId, Guid userId, IReadOnlyCollection<Guid> noteIds, CancellationToken cancellationToken = default)
    {
        if (noteIds.Count == 0)
        {
            return 0;
        }

        var notes = await _context.PendingUserNotes
            .Where(n => n.AgentId == agentId && (n.UserId == userId || n.UserId == null) && noteIds.Contains(n.Id))
            .ToListAsync(cancellationToken);

        if (notes.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var personal = new List<PendingUserNote>();
        var changed = 0;

        foreach (var note in notes)
        {
            if (note.UserId == null)
            {
                if (note.FirstDeliveredAt == null)
                {
                    note.FirstDeliveredAt = now;
                    changed++;
                }
            }
            else
            {
                personal.Add(note);
            }
        }

        if (personal.Count > 0)
        {
            _context.PendingUserNotes.RemoveRange(personal);
            changed += personal.Count;
        }

        if (changed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
