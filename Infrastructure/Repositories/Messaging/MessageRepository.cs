// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository implementation for message persistence including filtered queries and retention cleanup.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Domain.Models.Messaging;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Messaging;

public class MessageRepository : IMessageRepository
{
    private readonly DataBaseContext _context;

    public MessageRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _context.Messages
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        Guid? providerId, MessageDirection? direction, string? sender, int count, int offset)
    {
        var query = _context.Messages.AsQueryable();

        if (providerId.HasValue)
            query = query.Where(m => m.ProviderId == providerId.Value);

        if (direction.HasValue)
            query = query.Where(m => m.Direction == direction.Value);

        if (!string.IsNullOrWhiteSpace(sender))
            query = query.Where(m => m.Sender == sender);

        return await query
            .OrderByDescending(m => m.Timestamp)
            .Skip(offset)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetMessageCountAsync(Guid providerId)
    {
        return await _context.Messages.CountAsync(m => m.ProviderId == providerId);
    }

    public async Task AddAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
    }

    public async Task DeleteOldestMessagesAsync(Guid providerId, int retainCount)
    {
        var totalCount = await _context.Messages.CountAsync(m => m.ProviderId == providerId);
        if (totalCount <= retainCount)
            return;

        var idsToDelete = await _context.Messages
            .Where(m => m.ProviderId == providerId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(retainCount)
            .Select(m => m.Id)
            .ToListAsync();

        await _context.Messages
            .Where(m => idsToDelete.Contains(m.Id))
            .ExecuteDeleteAsync();
    }
}
