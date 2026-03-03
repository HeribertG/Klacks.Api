// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentSessionRepository : IAgentSessionRepository
{
    private readonly DataBaseContext _context;

    public AgentSessionRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AgentSession> GetOrCreateAsync(Guid agentId, string sessionId, string userId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AgentSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.SessionId == sessionId, cancellationToken);

        if (existing != null)
            return existing;

        var session = new AgentSession
        {
            AgentId = agentId,
            SessionId = sessionId,
            UserId = userId,
            Status = AgentSessionStatus.Active,
            LastMessageAt = DateTime.UtcNow
        };

        _context.AgentSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<List<AgentSessionMessage>> GetActiveMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.AgentSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null) return [];

        return await _context.AgentSessionMessages
            .Where(m => m.SessionId == sessionId && !m.IsCompacted)
            .OrderBy(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentSessionMessage> SaveMessageAsync(AgentSessionMessage message, CancellationToken cancellationToken = default)
    {
        _context.AgentSessionMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task UpdateSessionAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        _context.AgentSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTokenCountEstAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessionMessages
            .Where(m => m.SessionId == sessionId && !m.IsCompacted)
            .SumAsync(m => m.TokenCount ?? 0, cancellationToken);
    }

    public async Task CompactMessagesAsync(Guid sessionId, string summaryContent, CancellationToken cancellationToken = default)
    {
        var summaryMessage = new AgentSessionMessage
        {
            SessionId = sessionId,
            Role = "system",
            Content = summaryContent,
            IsCompacted = false
        };
        _context.AgentSessionMessages.Add(summaryMessage);
        await _context.SaveChangesAsync(cancellationToken);

        var oldMessages = await _context.AgentSessionMessages
            .Where(m => m.SessionId == sessionId && !m.IsCompacted && m.Id != summaryMessage.Id)
            .ToListAsync(cancellationToken);

        foreach (var msg in oldMessages)
        {
            msg.IsCompacted = true;
            msg.CompactedIntoId = summaryMessage.Id;
        }

        var session = await _context.AgentSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session != null)
        {
            session.CompactionCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AgentSession>> GetUserSessionsAsync(string userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Where(s => s.UserId == userId && s.Status == AgentSessionStatus.Active)
            .OrderByDescending(s => s.LastMessageAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task ArchiveStaleSessionsAsync(int daysInactive = 30, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysInactive);
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE agent_sessions SET status = {AgentSessionStatus.Archived}, update_time = NOW()
            WHERE status = {AgentSessionStatus.Active}
              AND last_message_at < {cutoff}
              AND is_deleted = false
            """, cancellationToken);
    }
}
